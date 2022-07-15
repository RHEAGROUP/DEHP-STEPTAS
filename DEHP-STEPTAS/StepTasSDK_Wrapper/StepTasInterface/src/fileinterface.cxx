// --------------------------------------------------------------------------------------------------------------------
// <copyright file="fileinterface.cxx" company="Open Engineering S.A.">
//    Copyright (c) 2022 Open Engineering S.A.
//
//    Author:  Ivan Fontaine
//
//    This file is part of DEHP STEP-TAS (STEP 3D CAD) adapter project.
//
//    The DEHP STEP-TAS is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 3 of the License, or (at your option) any later version.
//
//    The DEHP STEP-TAS is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public License
//    along with this program; if not, write to the Free Software Foundation,
//    Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#include <Step/BaseExpressDataSet.h>
#include <tas_arm/SPFReader.h>
#include <tas_arm_support/ExpressDataSet_tas_arm_support.h>
#include <tas_arm_support/MaterialPropertiesTable.h>

#include <cstdio>
#include <iostream>
#include <string>
//#include <sys/types.h>
//#include <sys/stat.h>

#include "interface.hxx"
#include "fileinterface.hxx"

using namespace std;

namespace
{
	sti::Point3D getPoint3D(tas_arm::Mgm_3d_cartesian_point* cp)
	{
		sti::Point3D pd;
		if (cp == nullptr)return pd;
		pd.x = cp->getX();
		pd.y = cp->getY();
		pd.z = cp->getZ();
		return pd;
	}

	sti::Direction getDirection(tas_arm::Mgm_3d_direction* cp)
	{
		sti::Direction pd;
		if (cp == nullptr)return pd;
		pd.x = cp->getX();
		pd.y = cp->getY();
		pd.z = cp->getZ();
		return pd;
	}

	bool isFile(std::string fileName)
	{
		struct stat statBuf;
		if (stat(fileName.c_str(), &statBuf) != -1)
		{
			if ((statBuf.st_mode & S_IFMT) == S_IFREG)
			{
				return true;
			}
		}
		return false;
	}

	std::string FlatVector(vector<Step::String>& v)
	{
		std::string os;
		for (Step::String s : v)
		{
			os = os + s.toUTF8() + '\n';
		}
		return os;
	};

	void Trace(string name)
	{
		cout << name << endl;
	}
}


std::string FileInterface::stringNrfRealQuantityType_unit(
	tas_arm::Nrf_real_quantity_type* nrfRealQuantityType)
{
	std::string result = "not set!";
	if (nrfRealQuantityType->testUnit())
	{
		tas_arm::Nrf_any_unit* unit = 0;
		unit = nrfRealQuantityType->getUnit();
		if (unit->testName())
		{
			tas_arm::nrf_non_blank_label name = unit->getName();
			result = name.toLatin1();
		}
	}
	return result;
}


double FileInterface::QuantityValuePrescription_value(
	tas_arm::Nrf_real_quantity_value_prescription* nrfRealQuantityValuePrescription)
{
	double value = 0.0;
	tas_arm::Nrf_real_quantity_type* quantityType = 0;
	quantityType = nrfRealQuantityValuePrescription->getQuantity_type();
	if (quantityType)
	{
		value = nrfRealQuantityValuePrescription->getVal();
	}
	return value;
}


void FileInterface::processNrfNamedObservableItem(
	tas_arm::Nrf_named_observable_item* namedObservableItem, sti::TasNode* node)
{
	node->id = namedObservableItem->getKey();

	Already(node->id);
	if (namedObservableItem->testId())

	{
		tas_arm::nrf_identifier id = namedObservableItem->getId();

		string latin = id.toLatin1();

		node->name = latin;
	}

	if (namedObservableItem->testName())
	{
		tas_arm::nrf_label name = namedObservableItem->getName();
		string latin = name.toLatin1();
		node->label = latin;
	}

	if (namedObservableItem->testDescription())

	{
		tas_arm::nrf_text description = namedObservableItem->getDescription();
		string latin = description.toLatin1();
		node->description = latin;
	}

	if (namedObservableItem->testItem_class())
	{
		tas_arm::Nrf_named_observable_item_class* itemClass = 0;
		itemClass = namedObservableItem->getItem_class();
		tas_arm::nrf_non_blank_label name = itemClass->getName();
		std::string latin = name.toLatin1();
		node->classType = latin;
	}
}


//
void FileInterface::processNrfNetworkNode(
	tas_arm::Nrf_network_node* nrfNetworkNode, TasNode* node)

{
	processNrfNamedObservableItem(nrfNetworkNode, node);
}

// process attributes of an Mgm_any_meshed_geometric_item
//
void FileInterface::processMgmAnyMeshedGeometricItem(
	tas_arm::Mgm_any_meshed_geometric_item* mgmAnyMeshedGeometricItem, Geometry* geo)

{
	processNrfNetworkNode(mgmAnyMeshedGeometricItem, geo);
}

// process attributes of an Mgm_compound_meshed_geomtric_item as one block
//
void FileInterface::processMgmCompoundMeshedGeometricItem(
	tas_arm::Mgm_compound_meshed_geometric_item* mgmCompoundMeshedGeometricItem, TasNode* geo)

{
	//processMgmAnyMeshedGeometricItem_fields(mgmCompoundMeshedGeometricItem,geo);

	// mgm_compound_meshed_geometric_item.geometric_items : LIST [1:?] of UNIQUE mgm_any_meshed_geometric_items
	//

	TasNode* cpnode = new TasNode();
	processNrfNamedObservableItem(mgmCompoundMeshedGeometricItem, cpnode);
	geo->addChild(cpnode);

	if (mgmCompoundMeshedGeometricItem->testGeometric_items())
	{
		tas_arm::List_Mgm_any_meshed_geometric_item_1_n& items = mgmCompoundMeshedGeometricItem->getGeometric_items();

		for (auto geoitem : items) {
			Step::Id id = geoitem->getKey();
			if (isAlready(id))continue;
			Already(id);
			std::string nodeType = geoitem->type();
			if (nodeType == "Mgm_compound_meshed_geometric_item")
			{
				tas_arm::Mgm_compound_meshed_geometric_item* item = m_dataSet->getMgm_compound_meshed_geometric_item(id);
				processMgmCompoundMeshedGeometricItem(item, cpnode);
			}

			if (nodeType == "Mgm_meshed_primitive_bounded_surface") {
				// Normally we should not get any of these as they should have been correctly handled by the compound meshed geometry exploration

				tas_arm::Mgm_meshed_primitive_bounded_surface* mgmMPBS = 0;
				mgmMPBS = m_dataSet->getMgm_meshed_primitive_bounded_surface(id);
				processMgmMeshedPrimitiveBoundedSurface(mgmMPBS, cpnode);
			}
		}
	}
}

// process attributes of an Mgm_meshed_geometric_model as one block
//
void FileInterface::processMgmMeshedGeometricModel(
	tas_arm::Mgm_meshed_geometric_model* mgmMeshedGeometricModel, TasNode* node)

{
	// mgm_meshed_geometric_model.root_item : mgm_any_meshed_geometric_item
	//
	if (mgmMeshedGeometricModel->testRoot_item())
	{
		tas_arm::Mgm_any_meshed_geometric_item* rootItem = 0;
		rootItem = mgmMeshedGeometricModel->getRoot_item();
		string typ = rootItem->type();
		if (typ == "Mgm_compound_meshed_geometric_item")
		{
			processMgmCompoundMeshedGeometricItem(dynamic_cast<tas_arm::Mgm_compound_meshed_geometric_item*> (rootItem), node);
		}
	}
}

// process complete details of an Mgm_meshed_geometric_model as one block
//

void FileInterface::processMgmQuadrilateral(
	tas_arm::Mgm_quadrilateral* mgmQuad, Quadrilateral* quad)
{
	quad->name = "Quadrilateral";
	quad->P1 = getPoint3D(mgmQuad->getP1());
	quad->P2 = getPoint3D(mgmQuad->getP2());
	quad->P3 = getPoint3D(mgmQuad->getP3());
	quad->P4 = getPoint3D(mgmQuad->getP4());
}

// process attributes of an Mgm_sphere as one block
//

void FileInterface::processMgmSphere(
	tas_arm::Mgm_sphere* mgmSphere, Sphere* sphere)

{
	sphere->name = "Sphere";
	sphere->P1 = getPoint3D(mgmSphere->getP1());
	sphere->P2 = getPoint3D(mgmSphere->getP2());
	sphere->P3 = getPoint3D(mgmSphere->getP3());

	if (mgmSphere->testRadius())
	{
		tas_arm::Nrf_real_quantity_value_prescription* prescription = 0;

		sphere->Radius = QuantityValuePrescription_value(mgmSphere->getRadius());
	}

	// mgm_sphere.base_truncation : nrf_real_quantity_value_prescription
	//
	if (!mgmSphere->testBase_truncation())
	{
		printf("\tmgm_sphere.base_truncation: not set! [MANDATORY]\n");
	}
	else
	{
		tas_arm::Nrf_real_quantity_value_prescription* prescription = 0;
		prescription = mgmSphere->getBase_truncation();
		Step::Id entityId = prescription->getKey();
	}

	// mgm_sphere.apex_truncation : nrf_real_quantity_value_prescription
	//
	if (mgmSphere->testApex_truncation())

	{
		tas_arm::Nrf_real_quantity_value_prescription* prescription = 0;

		sphere->ApexTruncation = QuantityValuePrescription_value(mgmSphere->getApex_truncation());
	}

	if (mgmSphere->testStart_angle())

	{
		sphere->StartAngle = QuantityValuePrescription_value(mgmSphere->getStart_angle());
	}

	if (mgmSphere->testEnd_angle())

	{
		sphere->StartAngle = QuantityValuePrescription_value(mgmSphere->getEnd_angle());
	}
}

// process attributes of an Mgm_rectangle as one block
//
void FileInterface::processMgmRectangle(
	tas_arm::Mgm_rectangle* mgmRectangle, Rectangle* rectangle)

{
	// mgm_rectangle.p1 : mgm_3d_cartesian_point
	//
	rectangle->name = "Rectangle";
	rectangle->P1 = getPoint3D(mgmRectangle->getP1());
	rectangle->P2 = getPoint3D(mgmRectangle->getP2());
	rectangle->P3 = getPoint3D(mgmRectangle->getP3());
}



void FileInterface::processMgmRotation(
	tas_arm::Mgm_rotation* mgmRotation, Geometry* geo)

{
	// mgm_rotation.axis : mgm_3d_direction
	//
	if (!mgmRotation->testAxis())
	{
		printf("\tmgm_rotation.axis: not set! [MANDATORY]\n");
	}
	else
	{
		tas_arm::Mgm_3d_direction* axis = 0;
		axis = mgmRotation->getAxis();
		AxisRotation rot;
		Direction dir = getDirection(axis);
		rot.axis = dir;
		geo->transformation = rot;
	}

	// mgm_rotation.angle : REAL
	//
	if (!mgmRotation->testAngle())
	{
		printf("\tmgm_rotation.angle: not set! [MANDATORY]\n");
	}
	else
	{
		Step::Real angle = 0;
		angle = mgmRotation->getAngle();
	}

	// mgm_rotation.quantity_type : nrf_real_quantity_type
	//
	if (!mgmRotation->testQuantity_type())
	{
		printf("\tmgm_rotation.quantity_type: not set! [MANDATORY]\n");
	}
	else
	{
		tas_arm::Nrf_real_quantity_type* quantityType = 0;
		quantityType = mgmRotation->getQuantity_type();
		Step::Id entityId = quantityType->getKey();
		std::string details = stringNrfRealQuantityType_unit(quantityType);
		printf("\tmgm_rotation.quantity_type: #%ld  -> unit='%s'\n", entityId, details.c_str());
	}
}

// process complete details of an Mgm_rotation_with_axes_fixed as one block
//

// process an Mgm_axis_transformation_sequence and work down hierarchy if needed
//
void FileInterface::processMgmAxisTransformationSequence(
	tas_arm::Mgm_axis_transformation_sequence* mgmAxisTransformationSequence, Geometry* geom)

{
	// skip transformation sequences
	return;
	//processMgmAxisTransformationSequence_fields(mgmAxisTransformationSequence,geom);

	//processMgmAxisTransformationSequenceB(mgmAxisTransformationSequence);

	if (mgmAxisTransformationSequence->testTransformation_sequence())
	{
		tas_arm::List_Mgm_translation_or_rotation_1_n& transforms = mgmAxisTransformationSequence->getTransformation_sequence();

		for (auto transform : transforms)
		{
			Geometry* nn = new Geometry(); nn->label = "Transformation";
			geom->addChild(nn);

			Step::Id entityId = transform->getKey();
			geom->id = entityId;
			std::string transformationType = transform->type();

			if (transformationType == "Mgm_rotation_with_axes_fixed")
			{
				tas_arm::Mgm_rotation_with_axes_fixed* rotation = 0;
				rotation = m_dataSet->getMgm_rotation_with_axes_fixed(entityId);
			}
		}
	}
}

// process an Mgm_axis_transformation and work down hierarchy if needed
//
void FileInterface::processMgmAxisTransformation(
	tas_arm::Mgm_axis_transformation* mgmAxisTransformation, Geometry* geom)

{
	if (mgmAxisTransformation == nullptr) {
		return;
	}
	std::string transformationType = mgmAxisTransformation->type();
	Step::Id entityId = mgmAxisTransformation->getKey();
	if (transformationType == "Mgm_axis_transformation_sequence")
	{
		tas_arm::Mgm_axis_transformation_sequence* sequence = 0;
		sequence = m_dataSet->getMgm_axis_transformation_sequence(entityId);
		processMgmAxisTransformationSequence(sequence, geom);
	}
	else
	{
		cout << "Transformation to do " << transformationType << endl;
		//
	}
}

// get and process an individual quantity value
//

double FileInterface::processQuantityValue(
	Step::RefPtr<tas_arm_support::MaterialPropertiesTable> materialPropertiesTable,
	Step::String environmentName,
	Step::String materialId,
	Step::String quantityName)
{
	return  materialPropertiesTable->getPropertyRealValues(environmentName, materialId, quantityName)[0];
}

//
void FileInterface::processSurfaceMaterial(
	tas_arm::Nrf_material* nrfMaterial, sti::ThermalMaterialProperties* materialnode)

{
	processNrfNamedObservableItem(nrfMaterial, materialnode);

	Step::RefPtr<tas_arm::Mgm_meshed_geometric_model> meshedGeometricModel =
		m_dataSet->getNetworkModelsFromRoot("thermal_radiative_conductive_model").at(0);

	tas_arm_support::MaterialTablesMap& materialTablesMap = m_dataSet->getMaterial_tables();

	Step::RefPtr<tas_arm_support::MaterialPropertiesTable> materialPropertiesTable =
		materialTablesMap[meshedGeometricModel.get()];

	Step::List<Step::String>& environmentNames = materialPropertiesTable->getEnvironment_names();
	Step::List<Step::String>::iterator environmentIter;

	Step::List<Step::String>& materialIds = materialPropertiesTable->getMaterial_ids();
	Step::List<Step::String>::iterator materialIter;

	Step::String quantityName;
	bool materialFound = false;
	for (environmentIter = environmentNames.begin(); environmentIter != environmentNames.end(); ++environmentIter)
	{
		Step::String environmentName = (*environmentIter);
		for (materialIter = materialIds.begin(); materialIter != materialIds.end(); ++materialIter)
		{
			Step::String materialId = (*materialIter);
			if (materialId == nrfMaterial->getId())
			{
				materialFound = true;
				printf("\t-> property_environment = '%s'\n", environmentName.toLatin1().c_str());

				materialnode->solarAbsorptance = processQuantityValue(materialPropertiesTable, environmentName, materialId, "solar_absorptance");
				materialnode->solarDirectTransmittance = processQuantityValue(materialPropertiesTable, environmentName, materialId, "solar_direct_transmittance");
				materialnode->solarDiffuseTransmittance = processQuantityValue(materialPropertiesTable, environmentName, materialId, "solar_diffuse_transmittance");
				materialnode->solarSpecularity = processQuantityValue(materialPropertiesTable, environmentName, materialId, "solar_specularity");
				materialnode->solarRefractionIndex = processQuantityValue(materialPropertiesTable, environmentName, materialId, "solar_refraction_index");
				materialnode->infraredEmittance = processQuantityValue(materialPropertiesTable, environmentName, materialId, "infra_red_emittance");
				materialnode->infraredDirectTransmittance = processQuantityValue(materialPropertiesTable, environmentName, materialId, "infra_red_direct_transmittance");
				materialnode->infraredDiffuseTransmittance = processQuantityValue(materialPropertiesTable, environmentName, materialId, "infra_red_diffuse_transmittance");
				materialnode->infraredSpecularity = processQuantityValue(materialPropertiesTable, environmentName, materialId, "infra_red_specularity");
				materialnode->infraredRefractionIndex = processQuantityValue(materialPropertiesTable, environmentName, materialId, "infra_red_refraction_index");
			}
		}
	}
}


void FileInterface::processBulkMaterial(
	tas_arm::Nrf_material* nrfMaterial, Material* material)

{
	processNrfNamedObservableItem(nrfMaterial, material);

	Step::RefPtr<tas_arm::Mgm_meshed_geometric_model> meshedGeometricModel =
		m_dataSet->getNetworkModelsFromRoot("thermal_radiative_conductive_model").at(0);
	tas_arm_support::MaterialTablesMap& materialTablesMap = m_dataSet->getMaterial_tables();

	Step::RefPtr<tas_arm_support::MaterialPropertiesTable> materialPropertiesTable =
		materialTablesMap[meshedGeometricModel.get()];

	Step::List<Step::String>& environmentNames = materialPropertiesTable->getEnvironment_names();
	Step::List<Step::String>::iterator environmentIter;

	Step::List<Step::String>& materialIds = materialPropertiesTable->getMaterial_ids();
	Step::List<Step::String>::iterator materialIter;

	Step::String quantityName;
	bool materialFound = false;
	for (environmentIter = environmentNames.begin(); environmentIter != environmentNames.end(); ++environmentIter)
	{
		Step::String environmentName = (*environmentIter);
		for (materialIter = materialIds.begin(); materialIter != materialIds.end(); ++materialIter)
		{
			Step::String materialId = (*materialIter);
			if (materialId == nrfMaterial->getId())
			{
				materialFound = true;

				material->massDensity = processQuantityValue(materialPropertiesTable, environmentName, materialId, "mass_density");
				material->specificHeatCapacity = processQuantityValue(materialPropertiesTable, environmentName, materialId, "constant_pressure_specific_heat_capacity");
				material->thermalConductivity = processQuantityValue(materialPropertiesTable, environmentName, materialId, "thermal_conductivity");
			}
		}
	}
}

void FileInterface::processMgmMeshedPrimitiveBoundedSurface(
	tas_arm::Mgm_meshed_primitive_bounded_surface* mgmMeshedPrimitiveBoundedSurface, TasNode* rnode)

{
	TasNode* node = new BoundedSurface();
	rnode->addChild(node);
	processNrfNamedObservableItem(mgmMeshedPrimitiveBoundedSurface, node);
	BoundedSurface* surface = nullptr;
	if (mgmMeshedPrimitiveBoundedSurface->testSurface())
	{
		tas_arm::Mgm_primitive_bounded_surface* mgmPrimitiveBoundedSurface = 0;
		mgmPrimitiveBoundedSurface = mgmMeshedPrimitiveBoundedSurface->getSurface();
		Step::Id entityId = mgmPrimitiveBoundedSurface->getKey();
		std::string surfaceType = mgmPrimitiveBoundedSurface->type();
		if (surfaceType == "Mgm_rectangle")
		{
			tas_arm::Mgm_rectangle* mgmRectangle = 0;
			mgmRectangle = m_dataSet->getMgm_rectangle(entityId);
			Rectangle* rect = new Rectangle();
			processMgmRectangle(mgmRectangle, rect);
			surface = rect;
		}
		else if (surfaceType == "Mgm_quadrilateral")
		{
			tas_arm::Mgm_quadrilateral* mgmQuad = 0;
			mgmQuad = m_dataSet->getMgm_quadrilateral(entityId);
			Quadrilateral* quad = new Quadrilateral();
			processMgmQuadrilateral(mgmQuad, quad);
			surface = quad;
		}

		else if (surfaceType == "Mgm_sphere")
		{
			tas_arm::Mgm_sphere* mgmSphere = 0;
			mgmSphere = m_dataSet->getMgm_sphere(entityId);
			Sphere* sphere = new Sphere();
			processMgmSphere(mgmSphere, sphere);
			surface = sphere;
		}
		else
		{
			surface = new BoundedSurface();
		}
		surface->id = entityId;
		surface->classType = surfaceType;
	}

	if (mgmMeshedPrimitiveBoundedSurface->testActive_side())
	{
		surface->activeside = (ActiveSide)mgmMeshedPrimitiveBoundedSurface->getActive_side();
	}

	if (mgmMeshedPrimitiveBoundedSurface->testTransformation())
	{
		tas_arm::Mgm_axis_transformation* mgmAxisTransformation = 0;
		mgmAxisTransformation = mgmMeshedPrimitiveBoundedSurface->getTransformation();
		processMgmAxisTransformation(mgmAxisTransformation, surface);
	}

	if (surface != nullptr)
	{
		node->addChild(surface);
	}
	else
		return;
	// The rest is surface related
	if (mgmMeshedPrimitiveBoundedSurface->testSide1_surface_material())
	{
		tas_arm::Nrf_material* material = 0;
		material = mgmMeshedPrimitiveBoundedSurface->getSide1_surface_material();
		surface->side1_material = material->getKey();
		surface->side1_material_name = material->getName().toLatin1();
	}

	if (mgmMeshedPrimitiveBoundedSurface->testSide2_surface_material())
	{
		tas_arm::Nrf_material* material = 0;
		material = mgmMeshedPrimitiveBoundedSurface->getSide2_surface_material();
		surface->side2_material = material->getKey();
		surface->side2_material_name = material->getName().toLatin1();
	}

	if (mgmMeshedPrimitiveBoundedSurface->testSide1_bulk_material())
	{
		tas_arm::Nrf_material* material = 0;
		material = mgmMeshedPrimitiveBoundedSurface->getSide1_bulk_material();
		surface->side1_material = material->getKey();
	}

	if (mgmMeshedPrimitiveBoundedSurface->testSide2_bulk_material())
	{
		tas_arm::Nrf_material* material = 0;
		material = mgmMeshedPrimitiveBoundedSurface->getSide2_bulk_material();
		surface->side1_material = material->getKey();
	}

	if (mgmMeshedPrimitiveBoundedSurface->testSide1_faces())
	{
		if (mgmMeshedPrimitiveBoundedSurface->testSide1_faces())
		{
			tas_arm::List_Mgm_face_1_n& faces = mgmMeshedPrimitiveBoundedSurface->getSide1_faces();
			TasNode* facenode1 = new TasNode();
			bool active = (surface->activeside == SIDE1 || surface->activeside == BOTH);
			const string actives = ((active) ? "Active)" : "Not Active)");
			facenode1->label = string("Side(") + actives;
			facenode1->name = "Side 1";
			facenode1->id = getNewId();
			facenode1->classType = surface->classType + "/Side1";
			surface->addChild(facenode1);

			for (auto face : faces)
			{
				Face* facenode = new Face();
				facenode1->addChild(facenode);
				if (face->testCorresponding_node()) {
					facenode->name = face.get()->getCorresponding_node()->getId().toLatin1();
					facenode->nrf_network_node = face.get()->getCorresponding_node()->getId().toLatin1();
					facenode->classType = face.get()->getCorresponding_node()->getClassType().getName();
					facenode->id = face.get()->getCorresponding_node()->getKey();
					facenode->label = "Node on Face";
					auto model = face.get()->getCorresponding_node()->getContaining_model();
					if (model->testName()) {
						facenode->nrf_model = model->getName().toLatin1();
					}

				}

				facenode->classType = face.get()->getClassType().getName();
				facenode->id = face.get()->getKey();
				if (facenode->id == 0) facenode->id = getNewId();
			}
		}
	}

	if (mgmMeshedPrimitiveBoundedSurface->testSide2_faces())
	{
		if (mgmMeshedPrimitiveBoundedSurface->testSide2_faces())

		{
			tas_arm::List_Mgm_face_1_n& faces = mgmMeshedPrimitiveBoundedSurface->getSide2_faces();
			TasNode* facenode2 = new TasNode();
			bool active = (surface->activeside == SIDE2 || surface->activeside == BOTH);
			const string actives = ((active) ? "Active)" : "Not Active)");
			facenode2->label = "Side(" + actives;

			facenode2->name = "Side 2";
			facenode2->id = getNewId();
			facenode2->classType = surface->classType + "/Side2";
			surface->addChild(facenode2);

			for (auto face : faces)
			{
				Face* facenode = new Face();
				facenode2->addChild(facenode);
				if (face->testCorresponding_node()) {
					facenode->name = face.get()->getCorresponding_node()->getId().toLatin1();
					facenode->nrf_network_node = face.get()->getCorresponding_node()->getId().toLatin1();
					facenode->classType = face.get()->getCorresponding_node()->getClassType().getName();
					facenode->label = "Node on Face";
					auto model = face.get()->getCorresponding_node()->getContaining_model();
					if (model->testName()) {
						facenode->nrf_model = model->getName().toLatin1();
					}
				}
				facenode->classType = face.get()->getClassType().getName();
				facenode->id = face.get()->getKey();
				if (facenode->id == 0) facenode->id = getNewId();
			}
		}
	}
}

// process an Mgm_meshed_geometric_model and work down hierarchy if needed
//
void FileInterface::processMeshedGeometricModel(
	tas_arm::Mgm_meshed_geometric_model* mgmMeshedGeometricModel, TasNode* rnode)

{
	// Model root object
	TasNode* node = new TasNode();
	node->id = getNewId();
	rnode->addChild(node);

	processNrfNamedObservableItem(mgmMeshedGeometricModel, node);
	Already(mgmMeshedGeometricModel->getKey());
	Already(mgmMeshedGeometricModel->getRoot_item()->getKey());

	if (mgmMeshedGeometricModel->testMaterials())
	{
		tas_arm::List_Nrf_material_0_n& materials = mgmMeshedGeometricModel->getMaterials();

		for (auto material : materials)
		{
			Step::Id entityId = material->getKey();
			tas_arm::Nrf_material* nrfMaterial = 0;
			nrfMaterial = m_dataSet->getNrf_material(entityId);
			Material* mat = new Material();
			addMaterial(entityId, mat);
		}
	}

	if (mgmMeshedGeometricModel->testNodes())
	{
		tas_arm::List_Nrf_network_node_0_n& nodes = mgmMeshedGeometricModel->getNodes();
		tas_arm::List_Nrf_network_node_0_n::iterator it;
		for (it = nodes.begin(); it != nodes.end(); ++it)
		{
			Step::Id entityId = (*it)->getKey();
			if (isAlready(entityId)) { continue; }
			Already(entityId);

			std::string nodeType = (*it)->type();
			if (nodeType == "Mgm_compound_meshed_geometric_item")
			{
				tas_arm::Mgm_compound_meshed_geometric_item* mgmCMGI = 0;
				mgmCMGI = m_dataSet->getMgm_compound_meshed_geometric_item(entityId);
				processMgmCompoundMeshedGeometricItem(mgmCMGI, node);
			}
			else if (nodeType == "Mgm_meshed_primitive_bounded_surface") {
				// Normally we should not get any of these as they should have been correctly handled by the compound meshed geometry exploration

				tas_arm::Mgm_meshed_primitive_bounded_surface* mgmMPBS = 0;
				mgmMPBS = m_dataSet->getMgm_meshed_primitive_bounded_surface(entityId);
				processMgmMeshedPrimitiveBoundedSurface(mgmMPBS, node);
			}
		}
	}
}

// process an Nrf_root and work down hierarchy if needed
//
void FileInterface::processNrfRootCollection(tas_arm::Nrf_root* nrfRoot)
{
	if (m_root->testRoot_models())
	{
		tas_arm::List_Nrf_network_model_0_n& models = m_root->getRoot_models();

		for (auto model : models)
		{
			tas_arm::Nrf_network_model* nrfNetworkModel = model.get();

			// not very object oriented to switch on a type field, but keeping it simple

			string networkModelType = model->type();
			if (networkModelType == "Mgm_meshed_geometric_model")
			{
				TasNode* node = new TasNode(); // LINK to root
				node->id = getNewId();
				m_rootnode = node;

				cout << "Process geo model " << endl;
				Step::Id entityId = model->getKey();
				if (isAlready(entityId)) { continue; }
				tas_arm::Mgm_meshed_geometric_model* mgmMeshedGeometricModel = 0;
				mgmMeshedGeometricModel = m_dataSet->getMgm_meshed_geometric_model(entityId);
				processMeshedGeometricModel(mgmMeshedGeometricModel, node);
			}
			else
			{
				//cout << "Process network " << endl;

				// processNrfNetworkModel(model.get(),node);
			}
		}
	}
}

void FileInterface::processDataSet()
{
	if (m_dataSet != nullptr) {
		Step::SPFHeader& header = m_dataSet->getHeader();
		Step::SPFHeader::FileName& fName = header.getFileName();
		Step::SPFHeader::FileSchema& schema = header.getFileSchema();
		m_fh.schema = FlatVector(schema.schemaIdentifiers);
		m_fh.author = FlatVector(fName.author);
		m_fh.name = fName.name.toUTF8();
		m_fh.organization = FlatVector(fName.organization);
		m_fh.originatingSystem = fName.originatingSystem.toUTF8();
		m_fh.preprocessorVersion = fName.preprocessorVersion.toUTF8();
		m_fh.authorization = fName.authorization.toUTF8();
		m_fh.timeStamp = fName.timeStamp.toUTF8();
		Step::SPFHeader::FileDescription fDescription = header.getFileDescription();
		m_fh.description = FlatVector(fDescription.description);
	};

	tas_arm::Nrf_root* nrfRoot = m_dataSet->getRoot();

	m_root = nrfRoot;
	processNrfRootCollection(nrfRoot);
}

void FileInterface::addMaterial(Step::Id theId, sti::Material* theMat)
{
	m_material_map[theId] = theMat;
}

sti::Material  FileInterface::getMaterial(Step::Id theId)

{
	sti::Material mat;
	auto it = m_material_map.find(theId);
	if (it == m_material_map.end()) {
		return mat;
	}
	else return *(*it).second;
}

// process one file.
// assume fileName refers to a valid STEP-TAS file.
//
bool FileInterface::processStepTasFile(const string& fileName)
{
	if (!isFile(fileName))
	{
		cerr << "cannot find file " << fileName << endl;
		return false;
	}

	m_dataSet = new tas_arm_support::ExpressDataSet_tas_arm_support();

	m_dataSet->loadP21File(fileName.c_str());
	if (m_dataSet.valid())
	{
		m_dataSet->instantiateAll();
		m_dataSet->registerLoadedStepTasArmDataset();
		processDataSet();
	}

	return true;
}

void FileInterface::PrintTree()
{
	PrintNode(m_rootnode, 0);
}

void  FileInterface::PrintNode(TasNode* node, int indent)
{
	int n = indent * 5;
	cout << string(n, ' ') << "Node Id  " << node->name << endl;
	cout << string(n, ' ') << "Node Label  " << node->label << endl;
	cout << string(n, ' ') << "Node Type  " << node->classType << endl;
	cout << string(n, ' ') << "Node Entity #" << node->id << endl;
	int cnt = 0;
	cout << string(n, ' ') << "Number of children: " << node->Children.size() << endl;
	indent++;
	for (TasNode* child : node->Children)
	{
		cout << string(n, ' ') << indent << "-" << cnt++ << " " << endl;
		PrintNode(child, indent);
	}
}

void FileInterface::Already(Step::Id theId)
{
	m_already_processed.insert(theId);
}

bool FileInterface::isAlready(Step::Id theId)
{
	return m_already_processed.find(theId) != m_already_processed.end();
}

FileHeader FileInterface::GetFileHeader() {
	return m_fh;
}

void FileInterface::SetRootNode(TasNode* rootnode) {
	m_rootnode = rootnode;
}
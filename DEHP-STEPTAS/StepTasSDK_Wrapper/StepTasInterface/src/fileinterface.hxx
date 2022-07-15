#pragma once
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="fileinterface.hxx" company="Open Engineering S.A.">
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
#include <Step/Types.h>
#include <tas_arm/SPFReader.h>
#include <tas_arm_support/ExpressDataSet_tas_arm_support.h>
#include <tas_arm_support/MaterialPropertiesTable.h>
#include "interface.hxx"
#include <set>
using namespace std;
using namespace sti;
class __declspec(dllexport)  FileInterface
{
	
public:

	TasNode GetRootNode() { return *m_rootnode; };
	void SetRootNode(TasNode* rootnode);
	FileHeader GetFileHeader();
	bool  processStepTasFile(const string& fileName);
	void PrintNode(TasNode* node, int indent);
	void PrintTree();
private:

	set<Step::Id> m_already_processed; // use to avoid duplicate tree items
	// STEP TAS DATA
	tas_arm::Nrf_root* m_root;
	TasNode* m_rootnode;
	Step::RefPtr<tas_arm_support::ExpressDataSet_tas_arm_support> m_dataSet = 0;
	//Material Map
	map<Step::Id, Material*> m_material_map;
	// Exchange DATA
	FileHeader m_fh;
	int owncounter = 0;
	int getNewId() { return owncounter--; }
	bool isAlready(Step::Id theId);
	void Already(Step::Id theId);
	void addMaterial(Step::Id theId, sti::Material* theMaterialNode);
	sti::Material getMaterial(Step::Id theId);// returns a MaterialNode instance
	void processDataSet();
	void processNrfRoot(
		tas_arm::Nrf_root* nrfRoot);

	//std::string stringNrfNetworkNode(tas_arm::Nrf_network_node *nrfNetworkNode,ThermalNode *tnode);

	void processNrfNamedObservableItem(
		tas_arm::Nrf_named_observable_item* namedObservableItem, sti::TasNode* node);

	string stringNrfRealQuantityType_unit(
		tas_arm::Nrf_real_quantity_type* nrfRealQuantityType);
	double QuantityValuePrescription_value(
		tas_arm::Nrf_real_quantity_value_prescription* nrfRealQuantityValuePrescription);
	string stringMgm3dCartesianPoint(
		tas_arm::Mgm_3d_cartesian_point* mgm3dCartesianPoint);
	string stringMgm3dDirection(
		tas_arm::Mgm_3d_direction* mgm3dDirection);
	
	void processNrfNetworkNode(
		tas_arm::Nrf_network_node* nrfNetworkNode, TasNode* node);

	void processMgmMeshedPrimitiveBoundedSurface(
		tas_arm::Mgm_meshed_primitive_bounded_surface* mgmMeshedPrimitiveBoundedSurface, TasNode* node);

	void processMgmAnyMeshedGeometricItem(
		tas_arm::Mgm_any_meshed_geometric_item* mgmAnyMeshedGeometricItem, Geometry* geo);

	void processMgmCompoundMeshedGeometricItem(
		tas_arm::Mgm_compound_meshed_geometric_item* mgmCompoundMeshedGeometricItem, TasNode* node);
	void processMgmQuadrilateral(
		tas_arm::Mgm_quadrilateral* mgmSphere, Quadrilateral* quad);
	void processMgmMeshedGeometricModel(
		tas_arm::Mgm_meshed_geometric_model* mgmMeshedGeometricModel, TasNode* node);

	void processMgmSphere(
		tas_arm::Mgm_sphere* mgmSphere, Sphere* sphere);
	void processMgmRectangle(
		tas_arm::Mgm_rectangle* mgmRectangle, Rectangle* rect);
	void processMgmFace(
		tas_arm::Mgm_face* mgmFace, Face* Face);
	void processMgmRotation(
		tas_arm::Mgm_rotation* mgmRotation, Geometry* geo);

	
	void processMgmAxisTransformationSequence(
		tas_arm::Mgm_axis_transformation_sequence* mgmAxisTransformationSequence, Geometry* geo);
	void processMgmAxisTransformation(
		tas_arm::Mgm_axis_transformation* mgmAxisTransformation, Geometry* geo);
	void processSurfaceMaterial(
		tas_arm::Nrf_material* nrfMaterial, ThermalMaterialProperties* mat);
	void processBulkMaterial(
		tas_arm::Nrf_material* nrfMaterial, Material* mat);
	
	double processQuantityValue(
		Step::RefPtr<tas_arm_support::MaterialPropertiesTable> materialPropertiesTable,
		Step::String environmentName,
		Step::String materialId,
		Step::String quantityName);
	

	void processMeshedGeometricModel(
		tas_arm::Mgm_meshed_geometric_model* mgmMeshedGeometricModel, TasNode* node);

	void processNrfRootCollection(tas_arm::Nrf_root* nrfRoot);
	
};
#pragma once
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="interface.hxx" company="Open Engineering S.A.">
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

// DEHP STEP-TAS Adapter
// Low level interface
// This low level interface is made to be used as a wrapper generator input that will allow it to be used from C#
// as such it is designed only to handle really simple object made out of standard data types.
// (c) 2022 OPEN ENGINEERING 

#include <string>
#include <vector>
class FileInterface;
using namespace std;

namespace sti
{

	enum NodeType {
		TASNODE,
		BOUNDEDSURFACE,
		FACE,
		RECTANGLE,
		QUADRILATERAL

	};
	enum DataStatus
	{
		Unchanged,
		Modified,
		Deleted
	};

	enum ActiveSide
	{ UNSET,
		NONE,
		SIDE1,
		SIDE2,
		BOTH
	};

	typedef unsigned long StepId;
	//

	class Point3D
	{
	public:
		Point3D() :x(0.0), y(0.0) ,z(0.0) {};
		double x;
		double y;
		double z;
	};
	class Direction
	{
	public:
		Direction() :x(0.0), y(0.0), z(0.0) {};
		double x;
		double y;
		double z;
	};

	// File Data Structural elements
	// These are just node in the tree, they have a label a no associated data
//#ifndef SWIG	
	 __declspec(dllexport)
//#endif	
	class TasNode
	{

	public:
		DataStatus status;
		long entity;
		long id;// the structural id - most of the time it will be the same as the stepid, can be changed for exemple in a diff, where both tree are merged

		std::string name;
		std::string classType; //type of the corresponding step - tas
		
		int source;
		TasNode* parent;
		std::string label;
		std::string description;

		void addChild(TasNode* child);
        int childrenCount();
		TasNode* getParent();
        TasNode* getChildNode(int idx);
		virtual NodeType getNodeType();

#ifndef SWIG	
        std::vector<TasNode*> Children;
#endif
		
	};

	// The datanodes contains addtional data that  is displayable/editable
	class DataNode : public TasNode
	{
		
	};

	// Structural node
	class Side : public TasNode
	{
	public:
		ActiveSide side;
	};

	class AxisTransformation
	{
	};

	class AxisPlacement : public AxisTransformation
	{
	public:
		Point3D location;
	};
	class AxisTranslation : public AxisTransformation
	{
	public:
		Direction direction; //we set it in mm
	};

	class AxisRotation : public AxisTransformation
	{
	public:
		double angle; //rotation in rads.
		Direction axis;// If the axis is set to 0,0,0 we have a "rotation with a axes fixed"
	};

	class AxisTransformationSequence : public AxisTransformation
	{
	public:
		std::vector<AxisTransformation> transformationsequence;
	};

	class Geometry : public DataNode
	{
	public:
		AxisTransformation transformation;
	};

	class Face :public TasNode
	{ 
	public:
		// The children of the Face are made out of      nrf_network_nodes
		std::string nrf_network_node;
		std::string nrf_model;
	virtual NodeType getNodeType();
	};

	class Material : public DataNode
	{
	public:
		double massDensity;
		double specificHeatCapacity;
		double thermalConductivity;
	};
	class ThermalMaterialProperties : public Material
	{
	public:
		std::string environment;
		std::string sid;
		ActiveSide side;
		double solarAbsorptance;
		double solarDirectTransmittance;
		double solarDiffuseTransmittance;
		double solarSpecularity;
		double solarRefractionIndex;
		double infraredEmittance;
		double infraredDirectTransmittance;
		double infraredDiffuseTransmittance;
		double infraredSpecularity;
		double infraredRefractionIndex;

	
	};

	class ThermalNode : public TasNode
	{
		string id;
	};

	/*
	Meshed Bounded surfaces.

	*/

	class BoundedSurface : public Geometry
	{
	public:
		ActiveSide activeside;
		StepId side1_material;
		string side1_material_name;
		StepId side2_material;
		string side2_material_name;
		double side1_thickness;
		double side2_thickness;
		int dir1_meshing;
		int dir2_meshing;
		virtual NodeType getNodeType();

	};

	class Cone : public BoundedSurface
	{
	public:
		Point3D P1;
		Point3D P2;
		Point3D P3;
		double Radius1;
		double Radius2;
		double StartAngle;
		double EndAngle;
		
	};

	class Triangle : public BoundedSurface
	{
	public:
		Point3D P1;
		Point3D P2;
		Point3D P3;

	};

	class Rectangle : public BoundedSurface
	{
	public:
		Point3D P1;
		Point3D P2;
		Point3D P3;
		NodeType getNodeType();
	};

	class Quadrilateral : public BoundedSurface
	{
	public:
		Point3D P1;
		Point3D P2;
		Point3D P3;
		Point3D P4;
		NodeType getNodeType();
	};

	class Disc : public BoundedSurface
	{
	public:
		Point3D P1;
		Point3D P2;
		Point3D P3;
		double InnerRadius;
		double OuterRadius;
		double StartAngle;
		double EndAngle;
	};

	class Cylinder : public BoundedSurface
	{
	public:
		Point3D P1;
		Point3D P2;
		Point3D P3;
		double Radius;
		double  StartAngle;
		double  EndAngle;
	};

	class Sphere : public BoundedSurface
	{
	public:
		Point3D P1;
		Point3D P2;
		Point3D P3;
		double Radius;
		double BaseTruncation;
		double ApexTruncation;
		double StartAngle;
		double EndAngle;
	};

	class Paraboloid : public BoundedSurface
	{
	public:
		Point3D P1;
		Point3D P2;
		Point3D P3;
		double Radius;
		double ApexTruncation;
		double  StartAngle;
		double  EndAngle;
	};

	class FileHeader
	{
	public:
		std::string name;
		std::string timeStamp;
		std::string author;
		std::string organization;
		std::string preprocessorVersion;
		std::string originatingSystem;
		std::string description;
		std::string authorization;
		std::string schema;
	};
		

	class FileData
	{
	public:
		FileHeader header;
		Geometry rootGeomtry;

		FileData(const std::string & filename);
		//bool getStatus();
		TasNode getRoot();
	private:
		FileInterface* finter;
	};
}

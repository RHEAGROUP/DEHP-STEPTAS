// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StepTasRowData.cs" company="Open Engineering S.A.">
//    Copyright (c) 2020-2021 Open Engineering S.A.
//
//    Authors: Juan Pablo Hernandez Vogt, Ivan Fontaine
//
//    Part of the code was based on the work performed by RHEA as result
//    of the collaboration in the context of "Digital Engineering Hub Pathfinder"
//    by Sam Gerené, Alex Vorobiev, Alexander van Delft and Nathanael Smiechowski.
//
//    This file is part of DEHP STEP-TAS adapter project.
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
//using STEP3DAdapter;
using System;

namespace DEHPSTEPTAS.ViewModel.Rows
{
    public class StepTasRowData
    {
        public TasNode Node { get; }

        public int ID { get => (int)Node.id; }

        /// <summary>
        /// Auxiliary parent index for tree control.
        /// </summary>
        public int ParentID { get => getParentId(); }

        private int getParentId()
        {
            if (Node.parent == null) return 0;
            return Node.parent.id;
        }

        /// <summary>
        /// Gets the part instance name
        /// </summary>
        /// <remarks>
        /// The instance is the part name and the usage id <see cref="STNodeRelation.id"/>
        /// representing a unique string for the part.
        /// </remarks>
        ///

        public StepTasRowData Parent { get; set; }

        /**<summary> Use to store a unique name made by using the name and a numeral suffix in case of several node having the same name
         * </summary>
         */
        public string UniqueName { get; set; }

        public string Path { get => getPath(); }

      

        /// <summary>
        /// Get Part name.
        /// </summary>
        public string Name { get => Node.name; }

        public string Sides { get => getSides(); }

        private String getSides()
        {
            if (Node is BoundedSurface)
            {
                return ((BoundedSurface)Node).activeside.ToString();
            }
            else return "";
        }

        /// <summary>
        /// Get short entity type.
        /// </summary>
        public string Type { get => Node.classType; }

        public string ThermalNodes { get => getNodes(); }

        public string InstancePath { get => GetSignature(); }

        public string MaterialName { get => getMaterialName(); }
        /** <summary>If the current node is a Face, return the thermal node linked to it </summary>*/



        /// <summary>
        /// Get STEP entity type.
        /// </summary>
        public string RepresentationType { get => Node.description; }

        /// <summary>
        /// Get STEP entity file Id.
        /// </summary>
        public String StepId { get => (Node.entity < 0) ? "" : $"{Node.classType}(#{Node.entity})"; }

        /// <summary>
        /// Compose a reduced description of the <see cref="STNode"/>
        /// </summary>
        public string Description => (Node.description == "") ? $"{Node.label}" : $"{Node.description}";

        /// <summary>
        /// Gets a label of association
        /// </summary>
        /// <remarks>
        /// Using as label the <see cref="STNodeRelation.id"/> instead
        /// <see cref="STNodeRelation.name"/> because it was the only unique value
        /// exported by the different CAD applications tested during developments.
        /// </remarks>

        public string RelationLabel
        {
            get => $"{Node?.label}";
        }

        /// <summary>
        /// Gets the Get STEP entity file Id of the relation (NAUO)
        /// </summary>
        public string RelationId { get => $"{Node?.label}"; }

        /** <summary>
         * Retrieves the signature of the node. It is basically the full path of the node, made using uniquenames.
         * </summary>
         */

        public string GetSignature()
        {
            return getPath() + "/" + Node.name;

            //return getPath() + Node.name;
        }


        private string getPath()
        {
            string path = "";
            TasNode curnode = Node;
            while (curnode.parent != null)
            {
                if (curnode.parent.name.Length > 0)
                {
                    path = curnode.parent.name + "/" + path;
                }
                curnode = curnode.parent;
            }

            return path;
        }

        public string getNode()
        {

            
            if(Node is Face)
                return ((Face)Node).nrf_network_node;
            else return "";
        }


        private string getMaterialName()
        {

            if (Node.classType.Contains("/Side"))
            {
                
                TasNode parent = Node.getParent();
                if (parent is BoundedSurface)    // SPA: add "bs" in order to avoid first line in the block? 
                {
                    BoundedSurface bs = (BoundedSurface)parent;
                    if (Node.classType.Contains("/Side1")) return bs.side1_material_name;
                    if (Node.classType.Contains("/Side2")) return bs.side2_material_name;

                }

            }

            return "";
        }
       


        public string getNodes()
        {
            string localnodes = getNode();
            if (!string.IsNullOrEmpty(localnodes))
            {
                return localnodes;
            }
            if (Node.classType.Contains("/Side"))
            {
                string subnodes = "";                 // SPA: It looks that we create this string but it is never used....
                for(int i = 0; i < Node.childrenCount(); i++)
                {
                    TasNode tn = Node.getChildNode(i);
                    if (tn is Face)
                    {
                        subnodes+= ((Face)tn).nrf_network_node;
                    }
                    if (i < (Node.childrenCount() - 1)) subnodes = subnodes + ",";

                }
            }
            return "";
        }



        public StepTasRowData(TasNode node)
        {
            this.Node = node;
            //    this.Relation = relation;
            this.UniqueName = this.Name;
            node.entity = node.id;

            //this.InstanceName; = string.IsNullOrWhiteSpace(this.RelationLabel) ?this.Name : $"{this.Name}({this.RelationLabel})";
            // this.InstancePath = string.IsNullOrWhiteSpace(parentPath) ? this.InstanceName : $"{parentPath}.{this.InstanceName}";

            
            /*
            this.Node = node;
            Random rnd = new Random();
            int num = rnd.Next();
            this.UniqueName = this.Name + num;
            node.entity = node.id;
            */


        }


    }
}
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TasDataOnElementBase.cs" company="Open Engineering S.A.">
//     Copyright (c) 2022 Open Engineering S.A.
//
//     Author: Ivan Fontaine, S. Paquay
//
//     Part of the code was based on the work performed by RHEA as result of the collaboration in
//     the context of "Digital Engineering Hub Pathfinder" by Sam Gerené, Alex Vorobiev, Alexander
//     van Delft and Nathanael Smiechowski.
//
//     This file is part of DEHP STEP-TAS (STEP 3D CAD) adapter project.
//
//     The DEHP STEP-TAS is free software; you can redistribute it and/or modify it under the
//     terms of the GNU Lesser General Public License as published by the Free Software Foundation;
//     either version 3 of the License, or (at your option) any later version.
//
//     The DEHP STEP-TAS is distributed in the hope that it will be useful, but WITHOUT ANY
//     WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
//     PURPOSE. See the GNU Lesser General Public License for more details.
//
//     You should have received a copy of the GNU Lesser General Public License along with this
//     program; if not, write to the Free Software Foundation, Inc., 51 Franklin Street, Fifth
//     Floor, Boston, MA 02110-1301, USA.
// </copyright>
// -


using CDP4Common.EngineeringModelData;
using DEHPSTEPTAS.StepTas;
using System;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DEHPSTEPTAS.Extraction
{
    using DEHPSTEPTAS.StepTas;



    public class TasDataOnElementBase
    {

        public class node : IEquatable<node>
        {

            public string number { get; set; }
            public string meshedsurface { get; set; }
            public string model { get; set; }
            public bool Equals(node other)
            {
                return number.Equals(other.number) && meshedsurface.Equals(other.meshedsurface);
            }

            public override int GetHashCode()
            {
                int hCode = number.GetHashCode() + meshedsurface.GetHashCode();
                return hCode.GetHashCode();
            }
        }


        private List<node> ThermalNodeList = new();
        public List<node> nodes { get => ThermalNodeList; }
        public string name { get; private set; }

        public double val { get; set; } = 0.0;


        /**
         * <summary>Find the node referenced by the step-tas reference parameter</summary>
         */

        private TasNode FindReferenceNode(string name, string[] path, TasNode rootnode)
        {
            List<TasNode> subtree = StepTasFile.FlatTree(rootnode);
            int pindex = 0;
            foreach (TasNode node in subtree)
            {
                if (pindex < path.Length - 1 && node.name == path[pindex])
                {
                    pindex++;

                }
                if (pindex == path.Length - 1 && node.name == name)
                {
                    return node;
                }
            }

            return null;
            /*if (path.Length == 1 && rootnode.name == name) return rootnode;
            string childname = path.First();
            for (int i = 0; i < rootnode.childrenCount(); i++)
            {
                TasNode node = rootnode.getChildNode(i);
                TasNode parent = node.parent;
                if (parent != null && childname == parent.name)
                {
                    path = path.Skip(1).ToArray();
                    return FindReferenceNode(name, path, node);
                }
            }
            return null;*/
        }





        private void FindThermalNodes(TasNode rootnode)
        {
            List<TasNode> subtree = StepTasFile.FlatTree(rootnode);
            string sname = "";
            foreach (var node in subtree)
            {
                if (node.getNodeType() == NodeType.BOUNDEDSURFACE)
                {
                    sname = node.name;
                }

                if (node is Face)
                {
                    Face af = (Face)node;
                    string nodenumber = af.nrf_network_node;
                    if (nodenumber != "")
                    {
                        node rnode = new();
                        rnode.number = nodenumber;
                        rnode.meshedsurface = sname;
                        rnode.model = af.nrf_model;
                        ThermalNodeList.Add(rnode);
                    }
                }
            }
            ThermalNodeList = ThermalNodeList.Distinct().ToList();
        }

        public TasDataOnElementBase(ParameterBase stepTasParam, StepTasFile file, ParameterBase propertyParam, string selectedStateName)
        {
            // select the finite state for Tas ref 
            ActualFiniteState fsTasRef = null;
            if (stepTasParam.StateDependence != null)
            {
                fsTasRef = stepTasParam.StateDependence.ActualState.Find(x => x.Name == selectedStateName);
            }

            var valSet = stepTasParam.QueryParameterBaseValueSet(null, fsTasRef);
            string spath = valSet.ActualValue[1].ToString();
            string[] path = spath.Split('/');
            string name = valSet.ActualValue[0].ToString();

            this.name = name;

            if (name != "-") // It means that the reference is not defined (--> impossible to retrieve the nodes)
            {
                TasNode referencednode = FindReferenceNode(name, path, file.GetRootNode());
                FindThermalNodes(referencednode);
            }

            if (propertyParam is not null)
            {
                if (propertyParam.StateDependence is not null)  // we retrieve the value
                {
                    var listOfState = propertyParam.StateDependence.ActualState;

                    foreach (var actualstate in listOfState)
                    {
                        if (actualstate.Name == selectedStateName)
                        {
                            var valueSet = propertyParam.QueryParameterBaseValueSet(null, actualstate);

                            /*
                                string value = null;
                     var pub = ((ParameterValueSetBase)valueSet).Published;
                    if (pub is not null)
                        value = pub[0];
                    else
                        value = valueSet.ActualValue[0];
                    if (value == "-")
                        this.val = 0.0;
                    else
                        this.val = Double.Parse(value);
                    */


                            var value = ((ParameterValueSetBase)valueSet).Published[0];
                            if (value == "-")
                                value = valueSet.ActualValue[0];
                            if (value == "-")
                                this.val = 0.0;
                            else
                                this.val = Double.Parse(value);
                        }
                    }

                    // Consider case where the state is not found????
                }
                else
                {
                    var valueSet = propertyParam.QueryParameterBaseValueSet(null, null);

                    /*
                    string value = null;
                    var pub = ((ParameterValueSetBase)valueSet).Published;
                    if (pub is not null)
                        value = pub[0];
                    else
                        value = valueSet.ActualValue[0];
                    if (value == "-")
                        this.val = 0.0;
                    else
                        this.val = Double.Parse(value);
                    */


                    var value = ((ParameterValueSetBase)valueSet).Published[0];
                    if (value == "-")
                        value = valueSet.ActualValue[0];
                    if (value == "-")
                        this.val = 0.0;
                    else
                        this.val = Double.Parse(value);

                }
            }
        }
    }
}
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HighLevelRepresentationBuilder.cs" company="Open Engineering S.A.">
//    Copyright (c) 2021 Open Engineering S.A.
// 
//    Author: Juan Pablo Hernandez Vogt
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

namespace DEHPSTEPTAS.Builds.HighLevelRepresentationBuilder
{
    using DEHPSTEPTAS.ViewModel.Rows;
    using NLog;
    using DEHPSTEPTAS.StepTas;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Self-referential data source content.
    /// 
    /// Using the following service columns:
    /// - Key Field --> Step3DPartTreeNode.ID
    /// - Parent Field --> Step3DPartTreeNode.ParentID
    /// </summary>
    public class HighLevelRepresentationBuilder : IHighLevelRepresentationBuilder
    {
        /// <summary>
        /// List of geometric parts.
        /// 
        /// A part could be the container of parts.
        /// </summary>
        private TasNode[] nodes;

        /// <summary>
        /// List of relations between geometric parts
        /// </summary>
        

        /// <summary>
        /// Helper structure to speedup tree searches.
        /// <seealso cref="FindPart(int)"/>
        /// </summary>
        private readonly Dictionary<int, TasNode> idToNodeMap = new Dictionary<int, TasNode>();

        private readonly Dictionary<string, int> nameDict = new();
        /// <summary>
        /// Helper structure to speedup tree searches.
        /// <seealso cref="InitializeAuxiliaryData"/>
        //private readonly Dictionary<int, NodeRelation> idToRelationMap = new Dictionary<int, STNodeRelation>();

        /// <summary>
        /// Helper structure to speedup tree searches.
        /// <seealso cref="FindChildren(int)"/>
        /// </summary>
       // private readonly Dictionary<int, List<(STNode, STNodeRelation)>> partChildren = new Dictionary<int, List<(STNode, STNodeRelation)>>();

        /// <summary>
        /// Keep track of Parts used as parent of an Assembly.
        /// </summary>
        //private readonly HashSet<int> relatedParts = new HashSet<int>();

        /// <summary>
        /// Keep track of Parts used as childs of an Assembly.
        /// </summary>
        //private readonly HashSet<int> relatingParts = new HashSet<int>();

        /// <summary>
        /// The current class <see cref="NLog.Logger"/>
        /// </summary>
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Creates the High Level Representation (HLR) View Model for STEP TAS file
        /// </summary>
        /// <remarks>
        /// HLR Tree construction:
        /// 
        /// Each Part could appears many times
        /// * As used in an Assembly
        /// * As that Assembly is also used in other Assemblies
        ///
        /// Assemblies (roots) trigger recursivity
        /// Parts (leaves) are processed normally
        /// Parts not referenced as target of any Assembly belongs to the main Root
        ///
        /// Each ParentID is associated to a PartRelation which must be stored
        /// in some place to retrieve the information of the specific association.
        ///
        /// The global identification of a Part instance is the full path of IDs.
        /// </remarks>
        
        public List<StepTasRowData> CreateHLR(StepTasFile steptasfile,int ofset=0)
        {
            if (steptasfile == null) return new List<StepTasRowData>();
            var entries = new List<StepTasRowData>();
            List<int> ids = new(); ;  // SPA: Why this structure?
            foreach (var n in steptasfile.nodelist)
            {

                var entry = new StepTasRowData(n);
                ids.Add(entry.ID);
           
                entries.Add(entry);

            }
            ids.Sort(); // SPA: WHY?
            
            
          //  int nextID = cntOffSet;
            /*
            foreach (var p in this.n)
            {
                if (IsIsolatedPart(p))
                {
                    // Orphan parts are added to the maint Root
                    var node = new StepTasRowData(nameDict,p, null) { ID = nextID++ };
                    entries.Add(node);

                    // Process parts of children
                    AddSubTree(entries, node, ref nextID);
                }
            }
            */
            return entries;
        }

        /// <summary>
        /// Fill the auxiliary HasSet/Dictionary to speedup the tree construction.
        /// </summary>
        /// <param name="parts">List of geometric parts</param>
        /// <param name="relations">List of part relations defining instances in the tree composition</param>
        private void InitializeAuxiliaryData(TasNode[] nodes)
        {
            this.nodes = nodes;
            //this.relations = relations;

            idToNodeMap.Clear();
           /* idToRelationMap.Clear();
            partChildren.Clear();  // Constructed at FindChildren() call
            relatingParts.Clear();
            relatedParts.Clear();

            // Fill auxiliary helper structures

            foreach (var p in this.parts)
            {
                idToNodeMap.Add(p.Id, p);
            }

            foreach (var r in this.relations)
            {
                idToRelationMap.Add(r.stepId, r);

                relatingParts.Add(r.relating_id);
                relatedParts.Add(r.related_id);
            }
           */
        }

        /// <summary>
        /// Verify if a Part does not belong to any other Part
        /// </summary>
        /// <param name="part">A <see cref="STNode"/></param>
        /// <returns>True is not used as related part</returns>
        

        /// <summary>
        /// Gets Children of a Part.
        /// </summary>
        /// <param name="parentId">StepId of a relating Part (parent)</param>
        /// <returns>List of child Part and PartRelation generating the instance</returns>
        
        /// <summary>
        /// Finds the <see cref="STNode"/> object from its StepId.
        /// </summary>
        /// <param name="partId">Step File Id</param>
        /// <returns>The <see cref="STNode"/> with requested Id</returns>
        private TasNode FindPart(int partId)
        {
            return idToNodeMap[partId];
        }

        /// <summary>
        /// Adds children of a tree node.
        /// </summary>
        /// <param name="entries">Tree container to fill</param>
        /// <param name="parent">Parent row node</param>
        /// <param name="nextID">Global tree ID for next creation operation</param>
      
    }
}

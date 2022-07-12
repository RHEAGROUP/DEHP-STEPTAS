// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StepTasFile.cs" company="Open Engineering S.A.">
//    Copyright (c) 2020-2021 Open Engineering S.A.
// 
//    Author: Ivan Fontaine
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

using System;
using System.Collections.Generic;

namespace DEHPSTEPTAS.StepTas
{
    public class StepTasFile
    {
        private FileData filed;
        private TasNode rootnode;
        public List<TasNode> nodelist;
        public bool HasFailed; // ADD GETTER
        public String ErrorMessage;
        public String FileName;
        public FileHeader HeaderInfo;
        /*
         * Method to transform the tree node structure into a flat list of nodes.
         */
                 
        static public  List<TasNode> FlatTree(TasNode root)
        {
            List<TasNode> nodes = new();
            nodes.Add(root);
            for (int i = 0; i < root.childrenCount(); i++)
            {
                nodes.AddRange(FlatTree(root.getChildNode(i)));   // SPA: recursive call to FlatTree
            }

            return nodes;
        }

        public StepTasFile(String filename)
        {
            this.FileName = filename;
            filed = new FileData(filename);
            rootnode = filed.getRoot().getChildNode(0);rootnode.parent = null;            
            HeaderInfo = filed.header;
            nodelist = FlatTree(rootnode);

        }
        public TasNode GetRootNode()
        {

            return filed.getRoot().getChildNode(0);
        }
    }
}
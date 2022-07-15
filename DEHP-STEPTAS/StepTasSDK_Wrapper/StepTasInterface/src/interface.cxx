// Strptas interface
// Interface class
#include "interface.hxx"
#include "fileinterface.hxx"

FileData::FileData(const std::string& filename)
{
	
	finter = new FileInterface();
	finter->processStepTasFile(filename);
	header = finter->GetFileHeader();

}

TasNode FileData::getRoot()
{
	return finter->GetRootNode();
}

void TasNode::addChild(TasNode* child) {
	
	Children.push_back(child);
	child->parent = this;
}

int TasNode::childrenCount()
{
	return Children.size();
}

TasNode* TasNode::getChildNode(int idx)
{
	if (idx<0 || idx>Children.size()) {
		
		return nullptr;
	}
	return Children[idx];
}

TasNode* TasNode::getParent()
{
	return parent;
}


NodeType TasNode::getNodeType()
{
	return TASNODE;
};

NodeType Face::getNodeType()
{
	return FACE;
};

NodeType BoundedSurface::getNodeType()
{
	return BOUNDEDSURFACE;
};

NodeType Rectangle::getNodeType()
{
	return RECTANGLE;
};

NodeType Quadrilateral::getNodeType()
{
	return QUADRILATERAL;
};
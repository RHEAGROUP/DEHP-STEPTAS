%module steptasinterface
#%rename(opEquals) operator==;
#%rename(opAdd) operator+;
#%rename(opAddEquals) operator+=;
#%rename(opLess) operator<;
#%rename(opDifferent) operator!=;
#%rename(opNot) operator!;
#%rename(opOr) operator||;
%{
#include "interface.hxx"
#include "fileinterface.hxx"
%}


%typemap(csout,excode=SWIGEXCODE) sti::TasNode*,TasNode,TasNode*{
    System.IntPtr cPtr = $imcall;
    $csclassname ret = ($csclassname) $modulePINVOKE.InstantiateConcreteNode(cPtr, $owner);$excode
    return ret;
}

%include "arrays_csharp.i"
%include "std_string.i"
%include "windows.i"
%include "std_vector.i"
%include "interface.hxx"



%pragma(csharp) imclasscode=%{

  public static TasNode  InstantiateConcreteNode(System.IntPtr cPtr, bool owner)
  {
    TasNode  ret = null;
    if (cPtr == System.IntPtr.Zero) {
      return ret;
    }
    NodeType type = (NodeType)$modulePINVOKE.TasNode_getNodeType(new System.Runtime.InteropServices.HandleRef(null, cPtr));
    switch (type) {
    case NodeType.TASNODE:
         ret = new TasNode(cPtr, owner);
         break;

    case NodeType.FACE:
         ret = new Face(cPtr, owner);
         break;
    case NodeType.BOUNDEDSURFACE:
        ret=new BoundedSurface(cPtr,owner);
        break;

    case NodeType.RECTANGLE:
        ret=new Rectangle(cPtr,owner);
      break;

    case NodeType.QUADRILATERAL:
        ret=new Quadrilateral(cPtr,owner);
      break;
    default:
        System.Diagnostics.Debug.Assert(false,
        System.String.Format("Encountered type '{0}' that is not known to be a TasNode concrete class",
            type.ToString()));
        break;
    }
  return ret;
  }
%}



#%typemap(csout):




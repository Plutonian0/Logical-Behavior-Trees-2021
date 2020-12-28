using Logical;
using System;
using UnityEngine;

/// <summary>
/// This is the NodeGraph class.
/// There is nothing you need to add here!
/// </summary>
public class TemplateNodeGraph : NodeGraph
{
}

/// <summary>
/// This is our unique GraphProperties class associated to a NodeGraph class!
/// We can define any variables that the node graph will need.
/// The properties defined in this class will exist in all instances of the owning NodeGraph class and will be
/// accessible through code from outside the graph instance as well.
/// The variables defined here follow the usual rules to Unity serialization.
/// </summary>
[GraphProperties(typeof(TemplateNodeGraph))]
public class TemplateGraphProperties : AGraphProperties
{
    /// <summary>
    /// Set this property to true if you wish to use the IMGUI property drawer when drawing this class
    /// in the graph editor's graph inspector panel.
    /// </summary>
    public override bool UseIMGUIPropertyDrawer { get { return false; } }

    //====================== Custom Variables Area ======================//
    public int ExampleInt = 0;
    public string ExampleString = "";

    public ExampleSerializedClass ExampleClass;

    [Serializable]
    public class ExampleSerializedClass
    {
        public int ExampleClassSampleInt = 1;
    }
    //===================================================================//
}

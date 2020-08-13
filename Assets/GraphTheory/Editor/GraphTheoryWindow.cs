﻿using GraphTheory.Editor.UIElements;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

//https://docs.unity3d.com/Manual/UIE-Events-Reference.html

namespace GraphTheory.Editor
{
    public class GraphTheoryWindow : EditorWindow
    {
        private const string DATA_STRING = "GraphWindowData";
        private const string TOOLBAR = "toolbar";
        private const string MAIN_SPLITVIEW = "main-TwoPanelSplit";
        private const string MAIN_PANEL_LEFT = "main-panel-left";
        private const string MAIN_PANEL_RIGHT = "main-panel-right";

        private GraphWindowData m_graphWindowData = null;
        private Toolbar m_toolbar = null;
        private TwoPaneSplitView m_mainSplitView = null;
        private TabGroupElement m_mainTabGroup = null;
        private LibraryTabElement m_libraryTab = null;
        private InspectorTabElement m_inspectorTab = null;
        private NodeGraphView m_nodeGraphView = null;

        private NodeGraph m_openedGraphInstance = null;
        
        /// <summary>
        /// Temp method to clear graph data for testing.
        /// </summary>
        [MenuItem("Graph/Clear Graph Data")]
        public static void ClearGraphData()
        {
            EditorPrefs.SetString(DATA_STRING, JsonUtility.ToJson(new GraphWindowData(), true));
        }

        /// <summary>
        /// When the UI is enabled, it sets up all the VisualElement references and loads in the window data.
        /// </summary>
        private void OnEnable() 
        {
            //==================================Load Initial Data======================================//
            var xmlAsset = Resources.Load<VisualTreeAsset>("GraphTheory/GraphTheoryWindow");
            xmlAsset.CloneTree(rootVisualElement);
            m_mainSplitView = rootVisualElement.Q<TwoPaneSplitView>(MAIN_SPLITVIEW);
            //=========================================================================================//

            //==================================Register Toolbar=======================================//
            m_toolbar = rootVisualElement.Q<Toolbar>(TOOLBAR);

            // Create Button
            var graphCreateButton = new ToolbarButton(() =>
            {
                CreateNewGraphPopup.OpenWindow();
            });
            graphCreateButton.text = "Create Graph";
            m_toolbar.Add(graphCreateButton);

            // Save Button
            var saveGraphButton = new ToolbarButton(() =>
            {
                if (m_openedGraphInstance != null)
                {
                    EditorUtility.SetDirty(m_openedGraphInstance);
                    AssetDatabase.SaveAssets();
                }
            });
            saveGraphButton.text = "Save";            
            m_toolbar.Add(saveGraphButton);

            var anythingButton = new ToolbarButton(() =>
            {
            });
            anythingButton.text = "test";
            m_toolbar.Add(anythingButton);
            //=========================================================================================//

            //====================================Register Panels======================================//
            // Left panel is dependent on the right (NodeGraphView) so ordering is important!
            VisualElement mainPanelRight = rootVisualElement.Q<VisualElement>(MAIN_PANEL_RIGHT);
            VisualElement mainPanelLeft = rootVisualElement.Q<VisualElement>(MAIN_PANEL_LEFT);

            // Populate right panel
            m_nodeGraphView = new NodeGraphView();
            m_nodeGraphView.StretchToParentSize();
            m_nodeGraphView.OnSelectionAdded += OnGraphElementSelectionAdded;
            m_nodeGraphView.OnSelectionRemoved += OnGraphElementSelectionRemoved;
            m_nodeGraphView.OnSelectionCleared += OnGraphElementSelectionCleared;
            mainPanelRight.Add(m_nodeGraphView);
            
            // Populate left panel
            List<(string, TabContentElement)> tabs = new List<(string, TabContentElement)>();
            tabs.Add(("Library", m_libraryTab = new LibraryTabElement((string guid) => { OpenGraph(guid); })));
            tabs.Add(("Inspector", m_inspectorTab = new InspectorTabElement()));
            m_mainTabGroup = new TabGroupElement(tabs);
            m_mainTabGroup.StretchToParentSize();
            mainPanelLeft.Add(m_mainTabGroup);
            //=========================================================================================//

            //==================================Callback Listeners=====================================//
            GraphModificationProcessor.OnGraphCreated += OnNewGraphCreated;
            GraphModificationProcessor.OnGraphWillDelete += OnGraphWillDelete;
            //=========================================================================================//

            // Deserialize the editor window data.
            DeserializeData();
        }

        /// <summary>
        /// Before closing window, save the editor window state and break any listeners.
        /// </summary>
        private void OnDisable()
        {
            SerializeData();

            GraphModificationProcessor.OnGraphCreated -= OnNewGraphCreated;
            GraphModificationProcessor.OnGraphWillDelete -= OnGraphWillDelete;
        }

        /// <summary>
        /// Retrieves editor window state from EditorPrefs and loads it
        /// </summary>
        private void DeserializeData()
        {
            // Get the serialized data from EditorPrefs.
            string serializedData = EditorPrefs.GetString(DATA_STRING, "");
            if(string.IsNullOrEmpty(serializedData))
            {
                m_graphWindowData = new GraphWindowData(); 
            }
            else
            {
                m_graphWindowData = JsonUtility.FromJson<GraphWindowData>(serializedData);
            }
            Debug.Log("Deserialized data: " + serializedData);
            
            // Load the data where necessary
            m_mainSplitView.SetSplitPosition(m_graphWindowData.MainSplitViewPosition);
            if(!string.IsNullOrEmpty(m_graphWindowData.OpenGraphGUID))
            {
                OpenGraph(m_graphWindowData.OpenGraphGUID);
            }
            m_mainTabGroup.DeserializeData(m_graphWindowData.MainTabGroup);
            
            // Load graph element selection
            List<string> selectedGraphElements = new List<string>(m_graphWindowData.SelectedGraphElements);
            m_graphWindowData.SelectedGraphElements.Clear();
            m_nodeGraphView.SetSelection(selectedGraphElements);
        }

        /// <summary>
        /// Update the editor window state class and serialize it into a string to be stored in EditorPrefs.
        /// </summary>
        private void SerializeData()
        {
            m_graphWindowData.MainSplitViewPosition = m_mainSplitView.SplitPosition;
            m_graphWindowData.MainTabGroup = m_mainTabGroup.GetSerializedData();

            Debug.Log("Serializing data: " + JsonUtility.ToJson(m_graphWindowData, true));
            EditorPrefs.SetString(DATA_STRING, JsonUtility.ToJson(m_graphWindowData, true));
        }

        /// <summary>
        /// Opens a specified graph and lets all the systems know.
        /// </summary>
        public void OpenGraph(string guid)
        {
            CloseCurrentGraph();

            m_openedGraphInstance = AssetDatabase.LoadAssetAtPath<NodeGraph>(AssetDatabase.GUIDToAssetPath(guid));
            if (m_openedGraphInstance != null)
            {
                m_graphWindowData.OpenGraphGUID = guid;
                m_libraryTab.SetOpenNodeGraph(m_openedGraphInstance, guid);
                m_inspectorTab.SetOpenNodeGraph(m_openedGraphInstance);
                m_nodeGraphView.SetNodeCollection(m_openedGraphInstance);
            }
        }

        /// <summary>
        /// Closes currently opened graph and updates editor window state.
        /// </summary>
        private void CloseCurrentGraph()
        {
            m_graphWindowData.OpenGraphGUID = "";
            m_openedGraphInstance = null;
            m_libraryTab.SetOpenNodeGraph(null, null);
            m_inspectorTab.SetOpenNodeGraph(null);
            m_nodeGraphView.SetNodeCollection(null);
        }

        /// <summary>
        /// Callback to call when new graph is created outside of the editor window.
        /// </summary>
        private void OnNewGraphCreated(NodeGraph graph)
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(graph, out string guid, out long localId);
            m_libraryTab.RegisterNewlyCreatedGraph(graph, guid);
            OpenGraph(guid);
        }

        /// <summary>
        /// Callback to call when graph is deleted from outside of the editor window.
        /// </summary>
        private void OnGraphWillDelete(NodeGraph graph)
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(graph, out string guid, out long localId);
            if (graph == m_openedGraphInstance)
            {
                CloseCurrentGraph();
            }
            m_libraryTab.HandleDeletedGraph(graph, guid);
        }

        /// <summary>
        /// Callback to call when an element is selected in the GraphView.
        /// </summary>
        private void OnGraphElementSelectionAdded(ISelectable addedElement)
        {
            if(m_nodeGraphView.selection.Count == 1)
            {
                if(addedElement is NodeView)
                {
                    m_inspectorTab.SetNode((addedElement as NodeView).NodeId);
                }
            }
            else
            {
                m_inspectorTab.SetNode("");
            }

            if(addedElement is NodeView)
            {
                m_graphWindowData.SelectedGraphElements.Add((addedElement as NodeView).NodeId);
            }
            if(addedElement is EdgeView)
            {
                m_graphWindowData.SelectedGraphElements.Add((addedElement as EdgeView).EdgeId);
            }
        }

        private void OnGraphElementSelectionRemoved(ISelectable removedElement)
        {
            if (removedElement is NodeView)
            {
                m_graphWindowData.SelectedGraphElements.Remove((removedElement as NodeView).NodeId);
            }
            if (removedElement is EdgeView)
            {
                m_graphWindowData.SelectedGraphElements.Remove((removedElement as EdgeView).EdgeId);
            }
        }

        private void OnGraphElementSelectionCleared()
        {
            m_graphWindowData.SelectedGraphElements.Clear();
            m_inspectorTab.SetNode("");
        }
    }
}
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Opc.Ua;
using Opc.Ua.Sample;
using Opc.Ua.Server;

namespace Quickstarts.Servers.LiHaSystem
{
    public class LiHaSystemNodeManagerFactory : INodeManagerFactory
    {
        /// <inheritdoc/>
        public INodeManager Create(IServerInternal server, ApplicationConfiguration configuration)
        {
            return new LiHaSystemNodeManager(server, configuration);
        }

        /// <inheritdoc/>
        public StringCollection NamespacesUris
        {
            get
            {
                var nameSpaces = new StringCollection {
                    Namespaces.LiHa,
                    Namespaces.LiHa + "/Instance"
                };
                return nameSpaces;
            }
        }
    }

    public class LiHaSystemNodeManager: SampleNodeManager
    {
        private LiHaConfiguration m_configuration;
        private ushort m_namespaceIndex;
        private long m_lastUsedId;

        #region Constructors
        /// <summary>
        /// Initializes the node manager.
        /// </summary>
        public LiHaSystemNodeManager(IServerInternal server, ApplicationConfiguration configuration)
            : base(server)
        {
            List<string> namespaceUris = new List<string>();

            namespaceUris.Add(Namespaces.LiHa);
            namespaceUris.Add(Namespaces.LiHa + "/Instance");

            NamespaceUris = namespaceUris;

            m_namespaceIndex = Server.NamespaceUris.GetIndexOrAppend(namespaceUris[1]);

            AddEncodeableNodeManagerTypes(typeof(LiHaSystemNodeManager).Assembly, typeof(LiHaSystemNodeManager).Namespace);

            // get the configuration for the node manager.
            m_configuration = configuration.ParseExtension<LiHaConfiguration>();

            // use suitable defaults if no configuration exists.
            if (m_configuration == null)
            {
                m_configuration = new LiHaConfiguration();
            }

            m_lastUsedId = 0;
        }
        #endregion

        #region INodeIdFactory Members
        /// <summary>
        /// Creates the NodeId for the specified node.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="node">The node.</param>
        /// <returns>The new NodeId.</returns>
        public override NodeId New(ISystemContext context, NodeState node)
        {
            uint id = Opc.Ua.Utils.IncrementIdentifier(ref m_lastUsedId);
            return new NodeId(id, m_namespaceIndex);
        }
        #endregion

        #region INodeManager Members
        /// <summary>
        /// Does any initialization required before the address space can be used.
        /// </summary>
        /// <remarks>
        /// The externalReferences is an out parameter that allows the node manager to link to nodes
        /// in other node managers. For example, the 'Objects' node is managed by the CoreNodeManager and
        /// should have a reference to the root folder node(s) exposed by this node manager.
        /// </remarks>
        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                LoadLiHaSystemNodeSets(externalReferences);
                base.CreateAddressSpace(externalReferences);
            }
        }

        /// <summary>
        /// Loads the LiHaSystem, Lads, usw. NodeSets
        /// </summary>
        /// <param name="externalReferences">External References.</param>
        private void LoadLiHaSystemNodeSets(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            ImportXml(externalReferences, @"C:\Softwareentwicklung\UA-.NETStandard\Applications\Quickstarts.Servers\LiHaSystem\Opc.Ua.Di.NodeSet2.xml");
            ImportXml(externalReferences, @"C:\Softwareentwicklung\UA-.NETStandard\Applications\Quickstarts.Servers\LiHaSystem\Opc.Ua.Machinery.NodeSet2.xml");
            ImportXml(externalReferences, @"C:\Softwareentwicklung\UA-.NETStandard\Applications\Quickstarts.Servers\LiHaSystem\lads.xml");
            ImportXml(externalReferences, @"C:\Softwareentwicklung\UA-.NETStandard\Applications\Quickstarts.Servers\LiHaSystem\lihasystem.xml");


            // NodeState folder = FindPredefinedNode(
            //     ExpandedNodeId.ToNodeId(ObjectIds.Boilers, Server.NamespaceUris),
            //     typeof(NodeState));


            // folder.AddReference(Opc.Ua.ReferenceTypeIds.Organizes, false, boiler.NodeId);
            // boiler.AddReference(Opc.Ua.ReferenceTypeIds.Organizes, true, folder.NodeId);


            //AddPredefinedNode(context, boiler);

            // Autostart boiler simulation state machine
            // MethodState start = boiler.Simulation.Start;
            // IList<Variant> inputArguments = new List<Variant>();
            // IList<Variant> outputArguments = new List<Variant>();
            // List<ServiceResult> errors = new List<ServiceResult>();
            // start.Call(context, boiler.NodeId, inputArguments, errors, outputArguments);
        }

        /// <summary>
        /// Import NodeSets from xml
        /// </summary>
        /// <param name="resourcepath">String to path of XML</param>
        private void ImportXml(IDictionary<NodeId, IList<IReference>> externalReferences, string resourcepath)
        {
            NodeStateCollection predefinedNodes = new NodeStateCollection();

            Stream stream = new FileStream(resourcepath, FileMode.Open);
            Opc.Ua.Export.UANodeSet nodeSet = Opc.Ua.Export.UANodeSet.Read(stream);

            SystemContext.NamespaceUris.Append(nodeSet.NamespaceUris.ToString());
            nodeSet.Import(SystemContext, predefinedNodes);

            for (int ii = 0; ii < predefinedNodes.Count; ii++)
            {
                AddPredefinedNode(SystemContext, predefinedNodes[ii]);
            }
            // ensure the reverse refernces exist.
            AddReverseReferences(externalReferences);
        }

        /// <summary>
        /// Loads a node set from a file or resource and addes them to the set of predefined nodes.
        /// </summary>
        protected override NodeStateCollection LoadPredefinedNodes(ISystemContext context)
        {
            NodeStateCollection predefinedNodes = new NodeStateCollection();
            //predefinedNodes.LoadFromBinaryResource(context, "Quickstarts.Servers.Boiler.Boiler.PredefinedNodes.uanodes", this.GetType().GetTypeInfo().Assembly, true);
            return predefinedNodes;
        }

        /// <summary>
        /// Replaces the generic node with a node specific to the model.
        /// </summary>
        protected override NodeState AddBehaviourToPredefinedNode(ISystemContext context, NodeState predefinedNode)
        {
            BaseObjectState passiveNode = predefinedNode as BaseObjectState;

            if (passiveNode == null)
            {
                return predefinedNode;
            }

            NodeId typeId = passiveNode.TypeDefinitionId;

            if (!IsNodeIdInNamespace(typeId) || typeId.IdType != IdType.Numeric)
            {
                return predefinedNode;
            }

            // switch ((uint)typeId.Identifier)
            // {
            //     case ObjectTypes.BaseObjectType:
            //     {
            //         if (passiveNode is BoilerState)
            //         {
            //             break;
            //         }
            //
            //         BoilerState activeNode = new BoilerState(passiveNode.Parent);
            //         activeNode.Create(context, passiveNode);
            //
            //         // replace the node in the parent.
            //         if (passiveNode.Parent != null)
            //         {
            //             passiveNode.Parent.ReplaceChild(context, activeNode);
            //         }
            //
            //         // Autostart boiler simulation state machine
            //         MethodState start = activeNode.Simulation.Start;
            //         IList<Variant> inputArguments = new List<Variant>();
            //         IList<Variant> outputArguments = new List<Variant>();
            //         List<ServiceResult> errors = new List<ServiceResult>();
            //         start.Call(context, activeNode.NodeId, inputArguments, errors, outputArguments);
            //
            //         return activeNode;
            //     }
            // }

            return predefinedNode;
        }

        /// <summary>
        /// Does any processing after a monitored item is created.
        /// </summary>
        protected override void OnCreateMonitoredItem(
            ISystemContext systemContext,
            MonitoredItemCreateRequest itemToCreate,
            MonitoredNode monitoredNode,
            DataChangeMonitoredItem monitoredItem)
        {
            // todo
        }

        /// <summary>
        /// Does any processing after a monitored item is created.
        /// </summary>
        protected override void OnModifyMonitoredItem(
            ISystemContext systemContext,
            MonitoredItemModifyRequest itemToModify,
            MonitoredNode monitoredNode,
            DataChangeMonitoredItem monitoredItem,
            double previousSamplingInterval)
        {
            // todo
        }

        /// <summary>
        /// Does any processing after a monitored item is deleted.
        /// </summary>
        protected override void OnDeleteMonitoredItem(
            ISystemContext systemContext,
            MonitoredNode monitoredNode,
            DataChangeMonitoredItem monitoredItem)
        {
            // todo
        }

        /// <summary>
        /// Does any processing after a monitored item is created.
        /// </summary>
        protected override void OnSetMonitoringMode(
            ISystemContext systemContext,
            MonitoredNode monitoredNode,
            DataChangeMonitoredItem monitoredItem,
            MonitoringMode previousMode,
            MonitoringMode currentMode)
        {
            // todo
        }
        #endregion
    }
}

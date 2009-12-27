using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows.Forms;
using XG.Client.TCP;
using XG.Core;

namespace XG.Client.TCP.WinForms
{
    public partial class MainWindow : Form
    {
        private TextDialog myDialog = new TextDialog();

        private RootObject myRootObject;

        private Dictionary<XGObject, TreeNode> myTreeNodes;
        private Dictionary<TreeNode, XGObject> myObjects;
        private List<XGObject> myCompleteObjects;
        private List<XGObject> myParentlessObjects;
        private List<string> myCompleteSearches;

        private Thread myThread;
        private TCPClient myClient;

        private BindingList<XGBot> myBotList;
        private BindingList<XGPacket> myPacketList;

        private XGObject myCurrentObject;
        private XGBot myCurrentBot;

        private delegate void NodeDelegate(TreeNode node1, TreeNode node2);
        private delegate void BoolDelegate(bool aBool);

        private event ObjectDelegate BotInserted;
        private event ObjectDelegate PacketInserted;

        #region MAIN

        public MainWindow()
        {
            InitializeComponent();
            this.Disposed += new EventHandler(Form1_Disposed);

            this.myTreeNodes = new Dictionary<XGObject, TreeNode>();
            this.myObjects = new Dictionary<TreeNode, XGObject>();
            this.myCompleteObjects = new List<XGObject>();
            this.myParentlessObjects = new List<XGObject>();
            this.myCompleteSearches = new List<string>();

            this.myBotList = new BindingList<XGBot>();
            this.myPacketList = new BindingList<XGPacket>();

            this.packetSource.DataSource = myPacketList;
            this.botSource.DataSource = myBotList;

            TreeNode tNode = new TreeNode("Downloads");
            tNode.ImageIndex = 2;
            tNode.SelectedImageIndex = 2;
            treeViewSpecial.Nodes.Add(tNode);

            tNode = new TreeNode("Open Slots");
            tNode.ImageIndex = 2;
            tNode.SelectedImageIndex = 2;
            treeViewSpecial.Nodes.Add(tNode);

            tNode = new TreeNode("Enabled Packets");
            tNode.ImageIndex = 2;
            tNode.SelectedImageIndex = 2;
            treeViewSpecial.Nodes.Add(tNode);

            this.BotInserted += new ObjectDelegate(_BotInserted);
            this.PacketInserted += new ObjectDelegate(_PacketInserted);
        }

        void _PacketInserted(XGObject aObj)
        {
            if (InvokeRequired)
            {
                this.Invoke(new ObjectDelegate(_PacketInserted), new object[] { aObj as XGPacket });
            }
            else
            {
                this.myPacketList.Add(aObj as XGPacket);
            }
        }

        void _BotInserted(XGObject aObj)
        {
            if (InvokeRequired)
            {
                this.Invoke(new ObjectDelegate(_BotInserted), new object[] { aObj as XGBot });
            }
            else
            {
                this.myBotList.Add(aObj as XGBot);
            }
        }

        #endregion

        #region CONNECT STUFF

        private void Connect()
        {
            this.myClient = new TCPClient();

			this.myClient.ConnectedEvent += new EmptyDelegate(myClient_Connected);
			this.myClient.DisconnectedEvent += new EmptyDelegate(myClient_Disconnected);
			this.myClient.ObjectAddedEvent += new ObjectObjectDelegate(myClient_ObjectAdded);
			this.myClient.ObjectChangedEvent += new ObjectDelegate(myClient_ObjectChanged);
			this.myClient.ObjectRemovedEvent += new ObjectDelegate(myClient_ObjectRemoved);

            this.myClient.Connect(this.tbServer.Text, int.Parse(this.tbPort.Text), this.tbPass.Text);
        }

        void myClient_Connected()
        {
        }

        void myClient_Disconnected()
        {
            this.Clear();
        }

        void myClient_ObjectAdded(XGObject aObj, XGObject aParent)
        {
            XGObject oldObj = this.myRootObject.getChildByGuid(aObj.Guid);
            if (oldObj != null)
            {
                XGHelper.CloneObject(aObj, oldObj, true);
            }
            else
            {
                XGObject parentObj = this.myRootObject.getChildByGuid(aObj.ParentGuid);
                if (parentObj != null)
                {
                    this.AddObject(aObj);
                    foreach (XGObject obj in this.myParentlessObjects.ToArray())
                    {
                        if (obj.ParentGuid == aObj.Guid)
                        {
                            this.myParentlessObjects.Remove(obj);
                            this.AddObject(obj);
                        }
                    }
                }
                else
                {
                    this.WriteData(TCPClientRequest.GetObject, aObj.ParentGuid, null);
                    this.myParentlessObjects.Add(aObj);
                }
            }
		}

		void myClient_ObjectChanged(XGObject aObj)
		{
			this.myClient_ObjectAdded(aObj, null);
		}

		void myClient_ObjectRemoved(XGObject aObj)
        {
                if (aObj.GetType() == typeof(XGServer))
                {
                    XGServer tServ = aObj as XGServer;
                    this.TreeObjectRemoved(tServ);
                    this.myRootObject.removeServer(tServ);
                }
                else if (aObj.GetType() == typeof(XGChannel))
                {
                    XGChannel tChan = aObj as XGChannel;
                    this.TreeObjectRemoved(tChan);
                    tChan.Parent.removeChannel(tChan);
                }
                else if (aObj.GetType() == typeof(XGBot))
                {
                    XGBot tBot = aObj as XGBot;
                    tBot.Parent.removeBot(tBot);
                }
                else if (aObj.GetType() == typeof(XGPacket))
                {
                    XGPacket tPack = aObj as XGPacket;
                    tPack.Parent.removePacket(tPack);
                }
        }

        private void Disconnect()
        {
            if (this.myClient.IsConnected)
            {
                this.myClient.Disconnect();
            }
            this.Clear();

            this.EnableConnectButton(true);
        }

        private void Clear()
        {
            if (this.myClient != null)
            {
				this.myClient.ConnectedEvent -= new EmptyDelegate(myClient_Connected);
				this.myClient.DisconnectedEvent -= new EmptyDelegate(myClient_Disconnected);
				this.myClient.ObjectAddedEvent -= new ObjectObjectDelegate(myClient_ObjectAdded);
				this.myClient.ObjectChangedEvent -= new ObjectDelegate(myClient_ObjectChanged);
				this.myClient.ObjectRemovedEvent -= new ObjectDelegate(myClient_ObjectRemoved);
            }

            try
            {
                this.treeViewObject.Nodes.Clear();
                this.myTreeNodes.Clear();
                this.myObjects.Clear();
                this.myCompleteObjects.Clear();
                this.myParentlessObjects.Clear();
                this.myCompleteSearches.Clear();
                this.myCurrentBot = null;
                this.myCurrentObject = null;
                this.myBotList.Clear();
                this.myPacketList.Clear();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void WriteData(TCPClientRequest aMessage, Guid aGuid, string aData)
        {
            //this.myClient.WriteData(aMessage, aGuid, aData);
        }

        private void AddObject(XGObject aObj)
        {
            if (aObj.GetType() == typeof(XGServer))
            {
                this.myRootObject.addServer(aObj as XGServer);
                this.TreeObjectAdded(null, aObj);

            }
            else if (aObj.GetType() == typeof(XGChannel))
            {
                XGServer tServ = this.myRootObject.getChildByGuid(aObj.ParentGuid) as XGServer;
                if (tServ != null)
                {
                    tServ.addChannel(aObj as XGChannel);
                    this.TreeObjectAdded(tServ, aObj);
                }
                else
                {
                    //this.WriteData(NetMessageClient.GetParentObject, guid.ToString());
                }

            }
            else if (aObj.GetType() == typeof(XGBot))
            {
                XGChannel tChan = this.myRootObject.getChildByGuid(aObj.ParentGuid) as XGChannel;
                if (tChan != null)
                {
                    XGBot tBot = aObj as XGBot;
                    tChan.addBot(tBot);

                    if (this.myCurrentObject == tBot.Parent || this.myCurrentObject == tBot.Parent.Parent)
                    {
                        if (!this.myBotList.Contains(tBot))
                        {
                            this.BotInserted(tBot);
                        }
                    }
                }
                else
                {
                    //this.WriteData(NetMessageClient.GetParentObject, guid.ToString());
                }

            }
            else if (aObj.GetType() == typeof(XGPacket))
            {
                XGBot tBot = this.myRootObject.getChildByGuid(aObj.ParentGuid) as XGBot;
                if (tBot != null)
                {
                    XGPacket tPack = aObj as XGPacket;
                    tBot.addPacket(tPack);

                    if (this.myCurrentBot == tBot)
                    {
                        if (!this.myPacketList.Contains(tPack))
                        {
                            this.PacketInserted(tPack);
                        }
                    }
                }
                else
                {
                    //this.WriteData(NetMessageClient.GetParentObject, guid.ToString());
                }
            }
        }

        #endregion

        #region TREENODESTUFF

        public void TreeObjectRemoved(XGObject aObj)
        {
            TreeNode tNode = this.myTreeNodes[aObj];

            this.myTreeNodes.Remove(aObj);
            this.myObjects.Remove(tNode);

            if (InvokeRequired)
            {
                this.Invoke(new NodeDelegate(RemoveNode), new object[] { tNode.Parent, tNode });
            }
            else
            {
                this.RemoveNode(tNode.Parent, tNode);
            }
        }

        public void TreeObjectAdded(XGObject aParent, XGObject aObj)
        {
            TreeNode tParentNode = null;
            try
            {
                tParentNode = this.myTreeNodes[aParent];
            }
            catch
            {
            }
            TreeNode tNode = new TreeNode(aObj.Name);

            this.myTreeNodes.Add(aObj, tNode);
            this.myObjects.Add(tNode, aObj);
            this.SetTreeNodeStuff(tNode);

            if (InvokeRequired)
            {
                this.Invoke(new NodeDelegate(AddNode), new object[] { tParentNode, tNode });
            }
            else
            {
                this.AddNode(tParentNode, tNode);
            }
        }

        private void SetTreeNodeStuff(TreeNode aNode)
        {
            XGObject tObj = this.myObjects[aNode];
            if (!tObj.Enabled)
            {
                aNode.ForeColor = Color.Gray;
            }
            else
            {
                aNode.ForeColor = Color.Black;
            }
            if (tObj.GetType() == typeof(XGServer))
            {
                aNode.ImageIndex = 0;
                aNode.SelectedImageIndex = 0;
                aNode.ContextMenuStrip = this.contextMenuServer;
            }
            else if (tObj.GetType() == typeof(XGChannel))
            {
                aNode.ImageIndex = 1;
                aNode.SelectedImageIndex = 1;
                aNode.ContextMenuStrip = this.contextMenuChannel;
            }
        }

        private void AddNode(TreeNode node1, TreeNode node2)
        {
            if (node1 == null)
            {
                treeViewObject.Nodes.Add(node2);
            }
            else
            {
                node1.Nodes.Add(node2);
            }
        }
        private void RemoveNode(TreeNode node1, TreeNode node2)
        {
            if (node1 == null)
            {
                treeViewObject.Nodes.Remove(node2);
            }
            else
            {
                node1.Nodes.Remove(node2);
            }
        }

        private void treeViewObject_AfterSelect(object sender, TreeViewEventArgs e)
        {
            TreeNode n = e.Node;
            XGObject obj = this.myObjects[n];
            if (obj != null)
            {
                //Thread t = new Thread( new ParameterizedThreadStart( UpdateTreeView ) );
                //t.Start( obj );
                UpdateTreeView(obj);
            }
        }

        private void treeViewSpecial_AfterSelect(object sender, TreeViewEventArgs e)
        {
            TreeNode n = e.Node;
            this.myPacketList.Clear();
            this.myBotList.Clear();

            if (this.myRootObject != null)
            {
                if (n.Text == "Downloads")
                {
                    foreach (XGServer server in this.myRootObject.Children)
                    {
                        foreach (XGChannel channel in server.Children)
                        {
                            foreach (XGBot bot in channel.Children)
                            {
                                if (bot.Connected)
                                {
                                    this.myBotList.Add(bot);
                                    foreach (XGPacket packet in bot.Children)
                                    {
                                        if (packet.Connected)
                                        {
                                            this.myPacketList.Add(packet);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else if (n.Text == "Open Slots")
                {
                    foreach (XGServer server in this.myRootObject.Children)
                    {
                        foreach (XGChannel channel in server.Children)
                        {
                            foreach (XGBot bot in channel.Children)
                            {
                                if (bot.InfoSlotTotal > 0 && bot.InfoSlotCurrent > 0)
                                {
                                    this.myBotList.Add(bot);
                                    foreach (XGPacket packet in bot.Children)
                                    {
                                        this.myPacketList.Add(packet);
                                    }
                                }
                            }
                        }
                    }
                }
                else if (n.Text == "Enabled Packets")
                {
                    foreach (XGServer server in this.myRootObject.Children)
                    {
                        foreach (XGChannel channel in server.Children)
                        {
                            foreach (XGBot bot in channel.Children)
                            {
                                bool add = false;
                                foreach (XGPacket packet in bot.Children)
                                {
                                    if (packet.Enabled)
                                    {
                                        this.myPacketList.Add(packet);
                                        add = true;
                                    }
                                }
                                if (add)
                                {
                                    this.myBotList.Add(bot);
                                }
                            }
                        }
                    }
                }
                else
                {
                    string search = n.Text.ToLower();
                    string[] searchList = search.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (!this.myCompleteSearches.Contains(search))
                    {
                        this.WriteData(TCPClientRequest.SearchPacket, Guid.Empty, search);
                        this.myCompleteSearches.Add(search);
                    }
                    else
                    {
                        foreach (XGServer server in this.myRootObject.Children)
                        {
                            foreach (XGChannel channel in server.Children)
                            {
                                foreach (XGBot bot in channel.Children)
                                {

                                    foreach (XGPacket packet in bot.Children)
                                    {
                                        if (packet.Name != null)
                                        {
                                            string name = packet.Name.ToLower();

                                            bool add = true;
                                            for (int i = 0; i < searchList.Length; i++)
                                            {
                                                if (!name.Contains(searchList[i]))
                                                {
                                                    add = false;
                                                    break;
                                                }
                                            }
                                            if (add)
                                            {
                                                if (!this.myBotList.Contains(packet.Parent))
                                                {
                                                    this.myBotList.Add(packet.Parent);
                                                }
                                                this.myPacketList.Add(packet);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void UpdateTreeView(object aObj)
        {
            if (aObj != null)
            {
                //if (aObj != this.myCurrentObject)
                //{
                this.myCurrentObject = aObj as XGObject;

                this.myBotList.Clear();
                if (aObj.GetType() == typeof(XGServer))
                {
                    foreach (XGChannel tChan in (aObj as XGServer).Children)
                    {
                        if (this.myCompleteObjects.Contains(tChan))
                        {
                            foreach (XGBot tBot in tChan.Children)
                            {
                                myBotList.Add(tBot);
                            }
                        }
                        else
                        {
                            this.WriteData(TCPClientRequest.GetChildrenFromObject, tChan.Guid, null);
                            this.myCompleteObjects.Add(tChan);
                        }
                    }
                }
                else if (aObj.GetType() == typeof(XGChannel))
                {
                    XGChannel tChan = aObj as XGChannel;
                    if (this.myCompleteObjects.Contains(tChan))
                    {
                        foreach (XGBot tBot in tChan.Children)
                        {
                            this.myBotList.Add(tBot);
                        }
                    }
                    else
                    {
                        this.WriteData(TCPClientRequest.GetChildrenFromObject, tChan.Guid, null);
                        this.myCompleteObjects.Add(tChan);
                    }
                }
                //}
            }
        }

        #endregion

        #region TREEVIEW CLICK HANDLING

        private void treeViewObjects_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                XGObject tObj = this.GetSelectedTreeObject();
                if (tObj != null)
                {
                    ContextMenuStrip menu = null;
                    if (tObj.GetType() == typeof(XGServer))
                    {
                        menu = this.contextMenuServer;
                    }
                    else if (tObj.GetType() == typeof(XGChannel))
                    {
                        menu = this.contextMenuChannel;
                    }
                    if (menu != null)
                    {
                        if (tObj.Enabled)
                        {
                            menu.Items[0].Enabled = false;
                            menu.Items[1].Enabled = true;
                        }
                        else
                        {
                            menu.Items[0].Enabled = true;
                            menu.Items[1].Enabled = false;
                        }
                    }
                }
            }
        }

        private void insertServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (DialogResult.OK == this.myDialog.ShowAsDialog("Please insert a Server name!"))
            {
                this.WriteData(TCPClientRequest.AddServer, Guid.Empty, this.myDialog.GetInput());
            }
        }

        private void insertChannelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (DialogResult.OK == this.myDialog.ShowAsDialog("Please insert a Channel name with #!"))
            {
                XGServer tServ = this.GetSelectedTreeObject() as XGServer;
                this.WriteData(TCPClientRequest.AddChannel, tServ.Guid, this.myDialog.GetInput());
            }
        }

        private void removeServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            XGServer tServ = this.GetSelectedTreeObject() as XGServer;
            if (tServ != null)
            {
                this.WriteData(TCPClientRequest.RemoveServer, tServ.Guid, null);
            }
        }

        private void removeChannelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            XGChannel tChan = this.GetSelectedTreeObject() as XGChannel;
            if (tChan != null)
            {
                this.WriteData(TCPClientRequest.RemoveChannel, tChan.Guid, null);
            }
        }

        private void removeSearchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode tNode = this.treeViewSpecial.SelectedNode;
            treeViewSpecial.Nodes.Remove(tNode);
        }

        private void enableObjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.FlipObjectState(this.GetSelectedTreeObject());
        }

        private void disableObjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.FlipObjectState(this.GetSelectedTreeObject());
        }

        #endregion

        #region GRIDVIEW STUFF

        private void botGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            XGBot tBot = null;
            if (botGridView.SelectedRows != null && botGridView.SelectedRows.Count > 0)
            {
                tBot = botGridView.SelectedRows[0].DataBoundItem as XGBot;
            }
            if (tBot != null)
            {
                //if (this.myCurrentBot != tBot)
                //{
                this.myCurrentBot = tBot;
                this.myPacketList.Clear();
                if (this.myCompleteObjects.Contains(tBot))
                {
                    /* TODO reimplement a search algo
                    List<XGObject> list = tBot.Children;
                    list.Sort(delegate(XGObject p1, XGObject p2)
                    {
                        return (p1 as XGPacket).Id.CompareTo((p2 as XGPacket).Id);
                    });
                    */
                    foreach (XGPacket pack in tBot.Children)
                    {
                        this.myPacketList.Add(pack);
                    }
                }
                else
                {
                    this.WriteData(TCPClientRequest.GetChildrenFromObject, tBot.Guid, null);
                    this.myCompleteObjects.Add(tBot);
                }
                //}
            }
        }

        private void packetGridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (botGridView.SelectedRows != null && botGridView.SelectedRows.Count > 0)
            {
                this.FlipObjectState(packetGridView.SelectedRows[0].DataBoundItem as XGPacket);
            }
        }

        #endregion

        #region SEARCH

        private void btnSearch_Click(object sender, EventArgs e)
        {
            this.Search(this.tbSearch.Text);
        }
        private void Search(string aSearch)
        {
            TreeNode tNode = new TreeNode(aSearch);
            tNode.ContextMenuStrip = this.contextMenuSearch;
            tNode.ImageIndex = 2;
            tNode.SelectedImageIndex = 2;
            treeViewSpecial.Nodes.Add(tNode);
        }

        #endregion

        #region OTHER EVENTS

        private void btnConnect_Click(object sender, EventArgs e)
        {
            this.EnableConnectButton(false);
            this.myThread = new Thread(new ThreadStart(Connect));
            this.myThread.Start();
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            this.Disconnect();
        }

        void Form1_Disposed(object sender, EventArgs e)
        {
            this.notifyIcon.Visible = false;
            this.Disposed -= new EventHandler(Form1_Disposed);
            this.Disconnect();
            Application.Exit();
        }

        private void showToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Visible = true;
        }

        private void hideToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Visible = false;
        }

        #endregion

        #region HELPER FUNCTIONS

        private void FlipObjectState(XGObject aObj)
        {
            if (aObj != null)
            {
				if (!aObj.Enabled)
                {
                    this.WriteData(TCPClientRequest.ActivateObject, aObj.Guid, null);
                }
                else
                {
                    this.WriteData(TCPClientRequest.DeactivateObject, aObj.Guid, null);
                }
            }
        }

        private XGObject GetSelectedTreeObject()
        {
            TreeNode n = this.treeViewObject.SelectedNode;
            try
            {
                return this.myObjects[n];
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void EnableConnectButton(bool aEnable)
        {
            if (InvokeRequired)
            {
                BoolDelegate enable = new BoolDelegate(EnableConnectButton);
                this.Invoke(enable, new object[] { aEnable });
            }
            else
            {
                this.btnConnect.Enabled = aEnable;
                this.btnDisconnect.Enabled = !aEnable;
            }
        }

        #endregion
    }
}

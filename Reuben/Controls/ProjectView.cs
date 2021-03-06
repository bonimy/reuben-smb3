﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Daiz.Library;
using Daiz.NES.Reuben.ProjectManagement;
namespace Daiz.NES.Reuben
{
    public partial class ProjectView : UserControl
    {
        private Dictionary<Guid, TreeNode> WorldToNodes;
        private Dictionary<Guid, TreeNode> LevelToNodes;
        private Dictionary<TreeNode, WorldInfo> NodesToWorlds;
        private Dictionary<TreeNode, LevelInfo> NodesToLevels;
        public ProjectView()
        {
            InitializeComponent();
            WorldToNodes = new Dictionary<Guid, TreeNode>();
            LevelToNodes = new Dictionary<Guid, TreeNode>();
            NodesToWorlds = new Dictionary<TreeNode, WorldInfo>();
            NodesToLevels = new Dictionary<TreeNode, LevelInfo>();

            ProjectController.ProjectManager.ProjectLoaded += new EventHandler<TEventArgs<Project>>(ProjectManager_ProjectLoaded);
            ProjectController.LevelManager.LevelAdded += new EventHandler<TEventArgs<LevelInfo>>(LevelManager_LevelAdded);
            ProjectController.WorldManager.WorldAdded += new EventHandler<TEventArgs<WorldInfo>>(WorldManager_WorldAdded);
        }

        void WorldManager_WorldAdded(object sender, TEventArgs<WorldInfo> e)
        {
            AddWorld(e.Data);
        }


        void LevelManager_LevelAdded(object sender, TEventArgs<LevelInfo> e)
        {
            AddLevel(e.Data);
        }

        void ProjectManager_ProjectLoaded(object sender, TEventArgs<Project> e)
        {
            RefreshProject();
        }

        TreeNode projectNode = new TreeNode();
        private void RefreshProject()
        {
            WorldToNodes.Clear();
            LevelToNodes.Clear();
            NodesToWorlds.Clear();
            NodesToLevels.Clear();
            TrvProjectView.Nodes.Clear();
            projectNode.Nodes.Clear();
            projectNode.Tag = ProjectController.ProjectManager.CurrentProject;
            projectNode.Text = ProjectController.ProjectManager.CurrentProject.Name + " (Project)";

            foreach(var w in from w in ProjectController.WorldManager.Worlds orderby w.Ordinal select w)
            {
                TreeNode nextNode = new TreeNode();
                nextNode.Text = w.Name;
                nextNode.Tag = w;
                foreach(var l in from lvl in ProjectController.LevelManager.Levels where lvl.WorldGuid == w.WorldGuid select lvl)
                {
                    TreeNode nextNextNode = new TreeNode();
                    nextNextNode.Text = l.Name;
                    LevelToNodes.Add(l.LevelGuid, nextNextNode);
                    NodesToLevels.Add(nextNextNode, l);
                }

                WorldToNodes.Add(w.WorldGuid, nextNode);
                NodesToWorlds.Add(nextNode, w);
                projectNode.Nodes.Add(nextNode);
                ToolStripMenuItem nextMenu = new ToolStripMenuItem(w.Name);
                nextMenu.Tag = w;
                MnuMoveTo.DropDownItems.Add(nextMenu);
                nextMenu.Click += new EventHandler(MoveLevelTo_Clicked);
            }

            TrvProjectView.Nodes.Add(projectNode);
            projectNode.Expand();
            TlsEdit.Enabled = false;

            foreach (var l in ProjectController.LevelManager.Levels)
            {
                if (l.BonusAreaFor == Guid.Empty)
                {
                    WorldToNodes[l.WorldGuid].Nodes.Add(LevelToNodes[l.LevelGuid]);
                }
                else
                {
                    if (!LevelToNodes.ContainsKey(l.BonusAreaFor))
                    {
                        l.BonusAreaFor = Guid.Empty;
                        WorldToNodes[l.WorldGuid].Nodes.Add(LevelToNodes[l.LevelGuid]);
                    }
                    else
                    {
                        LevelToNodes[l.BonusAreaFor].Nodes.Add(LevelToNodes[l.LevelGuid]);
                    }
                }
            }
        }

        private void MoveLevelTo_Clicked(object sender, EventArgs e)
        {
            WorldInfo wi = (sender as ToolStripMenuItem).Tag as WorldInfo;
            TreeNode oldWorldNode = WorldToNodes[(SelectedLevel.WorldGuid)];
            TreeNode newWorldNode = WorldToNodes[wi.WorldGuid];
            TreeNode lvlNode = LevelToNodes[SelectedLevel.LevelGuid];
            SelectedLevel.WorldGuid = wi.WorldGuid;
            oldWorldNode.Nodes.Remove(lvlNode);
            newWorldNode.Nodes.Add(lvlNode);
            
        }

        private void AddLevel(LevelInfo info)
        {
            TreeNode worldNode = WorldToNodes[info.WorldGuid];
            TreeNode nextLevelNode = new TreeNode();
            nextLevelNode.Tag = info;
            nextLevelNode.Text = info.Name;
            NodesToLevels.Add(nextLevelNode, info);
            LevelToNodes.Add(info.LevelGuid, nextLevelNode);
            worldNode.Nodes.Add(nextLevelNode);
            worldNode.Expand();
        }

        private void AddWorld(WorldInfo wi)
        {
            TreeNode nextNode = new TreeNode();
            nextNode.Text = wi.Name;
            nextNode.Tag = wi;
            WorldToNodes.Add(wi.WorldGuid, nextNode);
            NodesToWorlds.Add(nextNode, wi);
            projectNode.Nodes.Insert(projectNode.Nodes.Count - 1, nextNode);
            ToolStripMenuItem nextMenu = new ToolStripMenuItem(wi.Name);
            nextMenu.Tag = wi;
            MnuMoveTo.DropDownItems.Add(nextMenu);
            nextMenu.Click += new EventHandler(MoveLevelTo_Clicked);
        }

        private void TrvProjectView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            SelectedLevel = null;
            if (e.Node == null) return;
            TreeNode node = e.Node;
            if (NodesToWorlds.ContainsKey(node))
            {
                SelectionType = SelectionType.World;
                SelectedWorld = NodesToWorlds[node];
                TrvProjectView.ContextMenuStrip = CtxWorlds;
                TlsEdit.Enabled = !SelectedWorld.IsNoWorld;
            }
            else if (NodesToLevels.ContainsKey(node))
            {
                SelectionType = SelectionType.Level;
                SelectedLevel = NodesToLevels[node];
                SelectedWorld = ProjectController.WorldManager.GetWorldInfo(SelectedLevel.WorldGuid);
                TrvProjectView.ContextMenuStrip = CtxLevels;
                TlsEdit.Enabled = true;
                mnuMoveUp.Enabled = node.Index > 0;
                mnuMoveDown.Enabled = node.Index < node.Parent.Nodes.Count - 1;
            }
            else
            {
                SelectionType = SelectionType.Project;
                TrvProjectView.ContextMenuStrip = CtxWorlds;
                TlsEdit.Enabled = false;
            }
        }

        private void BtnChangeName_Click(object sender, EventArgs e)
        {
            if (TrvProjectView.SelectedNode == null) return;

            if (NodesToLevels.ContainsKey(TrvProjectView.SelectedNode))
            {
                TrvProjectView.SelectedNode.Text = SelectedLevel.Name;
            }
            else if (NodesToWorlds.ContainsKey(TrvProjectView.SelectedNode))
            {
                SelectedWorld.Name = SelectedWorld.Name;
                TrvProjectView.SelectedNode.Text = SelectedWorld.Name;
            }
            else
            {
                ProjectController.ProjectManager.CurrentProject.Name = ProjectController.ProjectName;
                TrvProjectView.SelectedNode.Text =  ProjectController.ProjectName + " (Project)";
            }
        }

        private void TrvProjectView_DoubleClick(object sender, EventArgs e)
        {
            if (SelectionType == SelectionType.Level)
            {
                Open();
            }
        }

        private void newLevelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectionType prevType = SelectionType;
            SelectionType = SelectionType.Level;
            New();
            SelectionType = prevType;
        }

        private LevelInfo SelectedLevel;
        private WorldInfo SelectedWorld;

        private void deleteLevelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectionType prevType = SelectionType;
            SelectionType = SelectionType.Level;
            Delete();
            SelectionType = prevType;
        }

        private void editWorldToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectionType prevType = SelectionType;
            SelectionType = SelectionType.World;
            Open();
            SelectionType = prevType;
        }

        private void New()
        {
            switch (SelectionType)
            {
                case SelectionType.Level:
                    ReubenController.CreateNewLevel(SelectedWorld);
                    break;


                case SelectionType.World:
                    ReubenController.CreateNewWorld();
                    break;
            }
        }

        private void Open()
        {
            switch (SelectionType)
            {
                case SelectionType.Level:

                    ReubenController.EditLevel(SelectedLevel);
                    break;

                case SelectionType.World:
                    ReubenController.EditWorld(SelectedWorld);
                    break;
            }
        }

        private void Delete()
        {
            ConfirmForm cForm = new ConfirmForm();

            cForm.StartPosition = FormStartPosition.CenterParent;
            cForm.Owner = ReubenController.MainWindow;

            switch (SelectionType)
            {
                case SelectionType.Level:
                    if (cForm.Confirm("Are you sure you want to permanently remove this level?"))
                    {
                        ReubenController.CloseLevelEditor(SelectedLevel);
                        LevelInfo li = SelectedLevel;
                        WorldToNodes[SelectedWorld.WorldGuid].Nodes.Remove(LevelToNodes[li.LevelGuid]);
                        LevelToNodes.Remove(li.LevelGuid);
                        ProjectController.LevelManager.RemoveLevel(li);
                    }
                    break;

                case SelectionType.World:

                    if (cForm.Confirm("Are you sure you want to permanently remove this world? All levels will be moved to No World."))
                    {
                        ReubenController.CloseWorldEditor(SelectedWorld);
                        ProjectController.WorldManager.RemoveWorld(SelectedWorld);
                        WorldInfo noWorld = ProjectController.WorldManager.GetNoWorld();
                        WorldInfo thisWorld = SelectedWorld;

                        foreach (TreeNode node in WorldToNodes[SelectedWorld.WorldGuid].Nodes)
                        {
                            LevelInfo li = NodesToLevels[node];
                            TreeNode oldWorldNode = WorldToNodes[SelectedWorld.WorldGuid];
                            TreeNode newWorldNode = WorldToNodes[noWorld.WorldGuid];
                            TreeNode lvlNode = LevelToNodes[li.LevelGuid];
                            li.WorldGuid = noWorld.WorldGuid;
                            oldWorldNode.Nodes.Remove(lvlNode);
                            newWorldNode.Nodes.Add(lvlNode);
                        }

                        projectNode.Nodes.Remove(WorldToNodes[thisWorld.WorldGuid]);
                        NodesToWorlds.Remove(WorldToNodes[thisWorld.WorldGuid]);
                        WorldToNodes.Remove(thisWorld.WorldGuid);
                        ToolStripMenuItem removeThis = null;
                        foreach (ToolStripMenuItem menu in MnuMoveTo.DropDownItems)
                        {
                            if (menu.Tag == thisWorld)
                            {
                                removeThis = menu;
                                break;
                            }
                        }

                        if (removeThis != null)
                        {
                            MnuMoveTo.DropDownItems.Remove(removeThis);
                        }
                    }
                    break;
            }
        }
                    
        SelectionType SelectionType;

        private void deleteWorldToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectionType prevType = SelectionType;
            SelectionType = SelectionType.World;
            Delete();
            SelectionType = prevType;
        }

        private void TsbNewLevel_Click(object sender, EventArgs e)
        {
            SelectionType prevType = SelectionType;
            SelectionType = SelectionType.Level;
            New();
            SelectionType = prevType;
            
        }

        private void TsbNewWorld_Click(object sender, EventArgs e)
        {
            SelectionType prevType = SelectionType;
            SelectionType = SelectionType.World;
            New();
            SelectionType = prevType;
        }

        private void newWorldToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectionType prevType = SelectionType;
            SelectionType = SelectionType.World;
            New();
            SelectionType = prevType;
        }

        private void TsbOpen_Click(object sender, EventArgs e)
        {
            Open();
        }

        private void TsbRename_Click(object sender, EventArgs e)
        {
            InputForm iForm = new InputForm();
            iForm.StartPosition = FormStartPosition.CenterParent;
            iForm.Owner = ReubenController.MainWindow;

            string name = iForm.GetInput("Please enter a new name");
            if (name != null)
            {
                switch (SelectionType)
                {
                    case SelectionType.Level:
                        SelectedLevel.Name = name;
                        LevelToNodes[SelectedLevel.LevelGuid].Text = name;
                        break;

                    case SelectionType.World:
                        SelectedWorld.Name = name;
                        WorldToNodes[SelectedWorld.WorldGuid].Text = name;
                        break;
                }
            }
        }

        private void TsbDelete_Click(object sender, EventArgs e)
        {
            Delete();
        }

        private void mnuMoveUp_Click(object sender, EventArgs e)
        {
            TreeNode tr = TrvProjectView.SelectedNode;
            TreeNode pa = tr.Parent;
            int index = pa.Nodes.IndexOf(tr);
            pa.Nodes.Remove(tr);
            pa.Nodes.Insert(index - 1, tr);
            TrvProjectView.SelectedNode = tr;
            LevelInfo li = NodesToLevels[tr];
            int liIndex = ProjectController.LevelManager.Levels.IndexOf(li);
            ProjectController.LevelManager.Levels.Remove(li);
            ProjectController.LevelManager.Levels.Insert(index - 1, li);
        }

        private void mnuMoveDown_Click(object sender, EventArgs e)
        {
            TreeNode tr = TrvProjectView.SelectedNode;
            TreeNode pa = tr.Parent;
            int index = pa.Nodes.IndexOf(tr);
            pa.Nodes.Remove(tr);
            pa.Nodes.Insert(index + 1, tr);
            TrvProjectView.SelectedNode = tr;
            LevelInfo li = NodesToLevels[tr];
            int liIndex = ProjectController.LevelManager.Levels.IndexOf(li);
            ProjectController.LevelManager.Levels.Remove(li);
            ProjectController.LevelManager.Levels.Insert(index + 1, li);
        }

        private void CtxLevels_Opening(object sender, CancelEventArgs e)
        {

        }

        private void MnuBonusArea_Click(object sender, EventArgs e)
        {
            LevelSelect ls = new LevelSelect();
            ls.ShowDialog();

            LevelInfo li = SelectedLevel;
            LevelToNodes[li.LevelGuid].Parent.Nodes.Remove(LevelToNodes[li.LevelGuid]);

            if (ls.DialogResult == DialogResult.Cancel)
            {
                li.BonusAreaFor = Guid.Empty;
                WorldToNodes[li.WorldGuid].Nodes.Add(LevelToNodes[li.LevelGuid]);
            }
            else
            {
                li.BonusAreaFor = ls.SelectedLevel.LevelGuid;
                LevelToNodes[li.BonusAreaFor].Nodes.Add(LevelToNodes[li.LevelGuid]);
            }
        }
    }

    public enum SelectionType
    {
        Project,
        World,
        Level
    }
}

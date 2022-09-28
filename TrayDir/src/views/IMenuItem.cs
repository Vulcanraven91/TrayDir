﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using System.Text.RegularExpressions;
using TrayDir.utils;

namespace TrayDir {
	public partial class IMenuItem {
		internal TrayInstance instance;
		internal TrayInstanceNode tiNode;
		internal TrayInstancePath tiPath;
		internal TrayInstanceVirtualFolder tiVirtualFolder;
		internal TrayInstancePlugin tiPlugin;

		public ToolStripMenuItem menuItem;
		public List<IMenuItem> folderChildren;
		public List<IMenuItem> nodeChildren = new List<IMenuItem>();
		public IMenuItem parent;
		public Bitmap menuIcon;

		private List<IMenuItem> dirMenuItems;
		private List<IMenuItem> fileMenuItems;

		public bool isErr { get { return tiPath != null ? !(Directory.Exists(tiPath.path)||File.Exists(tiPath.path)) : false; } }
		public bool isDir { get { return tiPath != null ? AppUtils.PathIsDirectory(tiPath.path) : false; } }
		public bool isFile { get { return tiPath != null ? AppUtils.PathIsFile(tiPath.path) : false; } } 
		public bool isVFolder { get { return tiVirtualFolder != null; } }
		public bool isPlugin { get { return tiPlugin != null; } }
		public bool loadedIcon = false;
		public bool enqueued = false;
		private bool assignedClickEvent = false;
		private bool painted = false;

		private string alias
		{
			get
			{
				if (tiPath != null)
				{
					return tiPath.alias;
				}
				if (tiVirtualFolder != null)
				{
					return tiVirtualFolder.alias;
				}
				if (tiPlugin != null)
				{
					return tiPlugin.alias;
				}
				return null;
			}
		}
		protected int depth
		{
			get
			{
				int d = 1;
				if (parent != null)
				{
					d += parent.depth;
				}
				return d;
			}
		}

		public IMenuItem(TrayInstance instance, TrayInstanceNode tiNode, TrayInstancePath path) : this(instance, tiNode, path, null, null, null) { }
		public IMenuItem(TrayInstance instance, TrayInstanceNode tiNode, TrayInstancePlugin plugin) : this(instance, tiNode, null, null, plugin, null) { }
		public IMenuItem(TrayInstance instance, TrayInstanceNode tiNode, TrayInstanceVirtualFolder virtualFolder) : this(instance, tiNode, null, virtualFolder, null, null) { }
		public IMenuItem(TrayInstance instance, TrayInstanceNode tiNode, TrayInstancePath tiPath, TrayInstanceVirtualFolder tiVirtualFolder, TrayInstancePlugin tiPlugin, IMenuItem parent)
		{
			this.instance = instance;
			this.tiNode = tiNode;
			this.tiPath = tiPath;
			this.tiVirtualFolder = tiVirtualFolder;
			this.tiPlugin = tiPlugin;
			this.parent = parent;
			if (tiPath != null) {
					folderChildren = new List<IMenuItem>();
					dirMenuItems = new List<IMenuItem>();
					fileMenuItems = new List<IMenuItem>();
			}
		}
		private void Clear() {
			RemoveChildren();
			menuIcon = null;
			painted = false;
			enqueued = false;
			loadedIcon = false;
			if (menuItem != null) {
				menuItem.DropDownItems.Clear();
				menuItem.Image = null;
				menuItem.Text = "";
			}
		}
		private void LoadFolderChildren(object sender, PaintEventArgs e)
		{
			if (!painted && isDir && !tiPath.shortcut) {
				MakeChildren();
				Load();
				menuItem.Invalidate();
			}
			painted = true;
		}
		private void MakeChildren()
		{
			if (isDir)
			{
				try
				{
					string[] dirpaths = Directory.GetFileSystemEntries(tiPath.path);
					foreach (string fp in dirpaths)
					{
						bool match = false;
						foreach(string regx in instance.regexList)
						{
							if (regx != string.Empty)
							{
								match = match || (Regex.Matches(fp, regx).Count > 0);
							}
							if (match) break;
						}
						if (!match)
						{
							folderChildren.Add(new IMenuItem(instance, null, new TrayInstancePath(fp), null, null, this));
						}
					}
				}
				catch { }
			}
		}
		private void MenuSave()
		{
			IMenuItem mi = parent;
			instance.view.tray.notifyIcon.ContextMenuStrip.Show();
			while (mi != null)
			{
				mi.menuItem.DropDown.AutoClose = false;
				mi.menuItem.DropDown.Show();
				mi.menuItem.Enabled = false;
				mi = mi.parent;
			}
			instance.view.tray.notifyIcon.ContextMenuStrip.AutoClose = false;
			instance.view.tray.notifyIcon.ContextMenuStrip.Enabled = false;
		}
		private int _clicks = 0;
		public void ResetClicks() {
			_clicks = 0;
			if (folderChildren != null) {
				foreach (IMenuItem m in folderChildren) {
					m.ResetClicks();
				}
			}
			if (nodeChildren != null) {
				foreach (IMenuItem m in nodeChildren) {
					m.ResetClicks();
				}
			}
			if (dirMenuItems != null) {
				foreach (IMenuItem m in dirMenuItems) {
					m.ResetClicks();
				}
			}
			if (fileMenuItems != null) {
				foreach (IMenuItem m in fileMenuItems) {
					m.ResetClicks();
				}
			}
		}
		// Grabbed from https://stackoverflow.com/questions/26587843/prevent-toolstripmenuitems-from-jumping-to-second-screen
		private void showContextMenu() {
			if (isFile || isDir || isVFolder ||  isPlugin) {
				MenuSave();
				Point pt = System.Windows.Forms.Cursor.Position;
				ContextMenuStrip cmnu = new ContextMenuStrip();
				ToolStripItem tsi;

				if (isFile || isPlugin) {
					tsi = cmnu.Items.Add(Properties.Strings.MenuItem_Run);
					tsi.Click += Run;
					tsi = cmnu.Items.Add(Properties.Strings.MenuItem_RunAdmin);
					tsi.Click += RunAs;
				}
				if (isDir || isFile || isPlugin) {
					tsi = cmnu.Items.Add(Properties.Strings.MenuItem_OpenFileExplorer);
					tsi.Click += Explore;
				}
				if (isDir || isFile) {
					tsi = cmnu.Items.Add(Properties.Strings.MenuItem_OpenCmd);
					tsi.Click += OpenCmd;
					tsi = cmnu.Items.Add(Properties.Strings.MenuItem_OpenCmdAdmin);
					tsi.Click += OpenAdminCmd;
				}
				if (isVFolder) {
					tsi = cmnu.Items.Add(Properties.Strings.MenuItem_RunAll);
					tsi.Click += RunAll;
				}
				cmnu.Show();
				cmnu.Location = pt;
				cmnu.Closing += MenuDestroy;
			}
		}
		internal void UpdateVisibility() {
			if ((isFile || isDir) && (tiPath != null)) {
				menuItem.Visible = tiPath.visible;
			} else if (isPlugin && (tiPlugin != null)) {
				menuItem.Visible = tiPlugin.visible;
			} else if (isVFolder) {
				menuItem.Visible = tiVirtualFolder.visible;
			}
		}
		public void Load()
		{
			if (menuItem == null)
			{
				menuItem = new ToolStripMenuItem();
				menuItem.DropDownOpening += MenuItemDropDownOpening;
				menuItem.DropDownOpening += LoadChildrenIconEvent;
				menuItem.Paint += LoadFolderChildren;
			}
			bool useAlias = (alias != null && alias != string.Empty);
			if (useAlias)
			{
				menuItem.Text = alias;
			}
			else
			{
				if (isDir)
				{
					menuItem.Text = new DirectoryInfo(tiPath.path).Name;
				}
				else if (isFile)
				{
					if (instance.settings.ShowFileExtensions)
					{
						menuItem.Text = Path.GetFileName(tiPath.path);
					}
					else
					{
						menuItem.Text = Path.GetFileNameWithoutExtension(tiPath.path);
					}
				}
				else if (tiPlugin != null)
				{
					TrayPlugin plugin = tiPlugin.plugin;
					if (plugin != null)
					{
						if (plugin.name == null || plugin.name == string.Empty)
						{
							menuItem.Text = "(plugin item)";
						}
						else
						{
							menuItem.Text = string.Format("({0})", plugin.name);
						}
					}
					else
					{
						menuItem.Text = "(plugin item)";
					}
				}
			}

			menuItem.DropDownItems.Clear();
			if (tiPath != null) {
				dirMenuItems?.Clear();
				fileMenuItems?.Clear();
				foreach (IMenuItem child in folderChildren) {
					child.Load();
					if (child.isDir) {
						dirMenuItems.Add(child);
					}
					if (child.isFile) {
						fileMenuItems.Add(child);
					}
				}
				if (isDir && tiPath.shortcut) {
					folderChildren.Clear();
					menuItem.DropDownItems.Clear();
				}
				if (folderChildren.Count == 0 && isDir && !tiPath.shortcut) {
					menuItem.DropDownItems.Add("(Empty)");
				}
				if (ProgramData.pd.settings.app.MenuSorting != "None") {
					if (ProgramData.pd.settings.app.MenuSorting == "Folders Top") {
						foreach (IMenuItem child in dirMenuItems) {
							menuItem.DropDownItems.Add(child.menuItem);
						}
						foreach (IMenuItem child in fileMenuItems) {
							menuItem.DropDownItems.Add(child.menuItem);
						}
					} else {
						foreach (IMenuItem child in fileMenuItems) {
							menuItem.DropDownItems.Add(child.menuItem);
						}
						foreach (IMenuItem child in dirMenuItems) {
							menuItem.DropDownItems.Add(child.menuItem);
						}
					}
				} else {
					foreach (IMenuItem child in folderChildren) {
						menuItem.DropDownItems.Add(child.menuItem);
					}
				}
			}
			if (!assignedClickEvent)
			{
				menuItem.MouseDown += MenuItemClick;
				menuItem.Click += MenuItemClick;
				assignedClickEvent = true;
			}
		}
		public void EnqueueImgLoad()
		{
			IMenuItemIconUtils.EnqueueIconLoad(this);
		}
		public void AddToCollection(ToolStripItemCollection collection)
		{
			collection.Add(menuItem);

			if (tiPath != null) {
				if (folderChildren.Count != menuItem.DropDownItems.Count) {
					menuItem.DropDownItems.Clear();
					dirMenuItems.Clear();
					fileMenuItems.Clear();

					if (ProgramData.pd.settings.app.MenuSorting != "None") {
						if (ProgramData.pd.settings.app.MenuSorting == "Folders Top") {
							foreach (IMenuItem child in dirMenuItems) {
								menuItem.DropDownItems.Add(child.menuItem);
							}
							foreach (IMenuItem child in fileMenuItems) {
								menuItem.DropDownItems.Add(child.menuItem);
							}
						} else {
							foreach (IMenuItem child in fileMenuItems) {
								menuItem.DropDownItems.Add(child.menuItem);
							}
							foreach (IMenuItem child in dirMenuItems) {
								menuItem.DropDownItems.Add(child.menuItem);
							}
						}
					} else {
						foreach (IMenuItem child in folderChildren) {
							menuItem.DropDownItems.Add(child.menuItem);
						}
					}
				}
			}
		}

		public void AddToCollectionExpanded(ToolStripItemCollection collection)
		{
			if (folderChildren.Count == 0) {
				MakeChildren();
				Load();
				LoadChildrenIconEvent(this, null);
			}
			if (folderChildren.Count > 0)
			{
				if (folderChildren.Count != menuItem.DropDownItems.Count)
				{
					menuItem.DropDownItems.Clear();
				}
				dirMenuItems.Clear();
				fileMenuItems.Clear();

				foreach (IMenuItem child in folderChildren)
				{
					if (child.isDir)
					{
						dirMenuItems.Add(child);
					}
					else if (child.isFile)
					{
						fileMenuItems.Add(child);
					}
				}
				if (ProgramData.pd.settings.app.MenuSorting != "None")
				{
					if (ProgramData.pd.settings.app.MenuSorting == "Folders Top")
					{
						foreach (IMenuItem child in dirMenuItems)
						{
							collection.Add(child.menuItem);
						}
						foreach (IMenuItem child in fileMenuItems)
						{
							collection.Add(child.menuItem);
						}
					}
					else
					{
						foreach (IMenuItem child in fileMenuItems)
						{
							collection.Add(child.menuItem);
						}
						foreach (IMenuItem child in dirMenuItems)
						{
							collection.Add(child.menuItem);
						}
					}
				}
				else
				{
					foreach (IMenuItem child in folderChildren)
					{
						collection.Add(child.menuItem);
					}
				}
			}
			else
			{
				collection.Add(menuItem);
			}
		}
		internal void RemoveChildren() {
			RemoveChildren(nodeChildren);
			if (tiPath != null) {
				RemoveChildren(folderChildren);
				RemoveChildren(dirMenuItems);
				RemoveChildren(fileMenuItems);
			}
			parent = null;
		}
		internal void RemoveChildren(List<IMenuItem> list) {
			while(list.Count > 0) {
				IMenuItem child = list[0];
				child.RemoveChildren();
				list.RemoveAt(0);
			}
			list.Clear();
		}
		public void Refresh() {
			Clear();
			LoadFolderChildren(null, null);
			Load();
		}
	}
}

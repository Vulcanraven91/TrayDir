﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace TrayDir
{
    public partial class IPluginForm : Form
    {
        Dictionary<string, int> pluginIndex = new Dictionary<string, int>();
        List<Control> labels = new List<Control>();
        List<Control> controls = new List<Control>();
        TrayPlugin selectedPlugin;
        TrayInstancePlugin tip;
        public IPluginForm(TrayInstancePlugin tip)
        {
            InitializeComponent();
            this.tip = tip;
            selectedPlugin = tip.plugin;
            LoadPlugins();
        }
        public void LoadPlugins()
        {
            foreach (TrayPlugin tp in ProgramData.pd.plugins)
            {
                string t = tp.getSignature();
                pluginIndex[t] = ProgramData.pd.plugins.IndexOf(tp);
                int i = pluginComboBox.Items.Add(t);

            }
            for (int i = 0; i < pluginComboBox.Items.Count; i++)
            {
                TrayPlugin tp = ProgramData.pd.plugins[pluginIndex[pluginComboBox.Items[i].ToString()]];
                if (tp == selectedPlugin)
                {
                    pluginComboBox.SelectedIndex = i;
                    break;
                }
            }
            aliasEdit.Text = tip.alias;
        }
        private void pluginComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selected = pluginComboBox.SelectedItem.ToString();
            tip.id = pluginIndex[selected];
            TrayPlugin tp = tip.plugin;
            selectedPlugin = tp;
            foreach (Control c in controls)
            {
                pluginTableLayoutPanel.Controls.Remove(c);
            }
            controls.Clear();
            foreach (Control c in labels)
            {
                pluginTableLayoutPanel.Controls.Remove(c);
            }
            labels.Clear();
            if (tp.parameterCount > 0)
            {
                for (int i = 0; i < tp.parameterCount; i++)
                {
                    Label l = new Label();
                    l.Text = String.Format("Parameter {0}", i + 1);
                    pluginTableLayoutPanel.Controls.Add(l, 0, (i + 2) * 2);
                    l.AutoSize = true;
                    labels.Add(l);
                    TextBox tb = new TextBox();
                    tb.Dock = DockStyle.Top;
                    tb.AutoSize = true;
                    controls.Add(tb);
                    pluginTableLayoutPanel.SetColumnSpan(tb, 2);
                    pluginTableLayoutPanel.Controls.Add(tb, 0, (i + 2) * 2 + 1);
                    TrayInstancePluginParameter tipp;
                    if (tip.parameters.Count < i + 1)
                    {
                        tipp = new TrayInstancePluginParameter();
                        tip.parameters.Add(tipp);
                    }
                    else
                    {
                        tipp = tip.parameters[i];
                        tb.Text = tipp.value;
                    }
                    tb.TextChanged += new EventHandler(delegate (object obj, EventArgs args)
                    {
                        tipp.value = tb.Text;
                    });
                }
            }
            for (int i = 0; i < pluginTableLayoutPanel.RowCount; i++)
            {
                RowStyle rs;
                if (pluginTableLayoutPanel.RowStyles.Count > i + 1)
                {
                    rs = pluginTableLayoutPanel.RowStyles[i];
                }
                else
                {
                    rs = new RowStyle();
                    pluginTableLayoutPanel.RowStyles.Add(rs);
                }
                rs.SizeType = SizeType.AutoSize;
            }
        }

        private void IPluginForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (selectedPlugin != null)
            {
                List<TrayInstancePluginParameter> par = new List<TrayInstancePluginParameter>();
                for (int i = selectedPlugin.parameterCount; i < tip.parameters.Count; i++)
                {
                    par.Add(tip.parameters[i]);
                }
                foreach (TrayInstancePluginParameter p in par)
                {
                    tip.parameters.Remove(p);
                }
            }
            tip.alias = aliasEdit.Text;
        }
    }
}
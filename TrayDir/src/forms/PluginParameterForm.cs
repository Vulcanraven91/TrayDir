﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace TrayDir
{
    public partial class PluginParameterForm : Form
    {
        TrayPlugin tp;
        TrayPluginParameter tpp {
            get {
                return tp.parameters[parameterComboBox.SelectedIndex];
            }
        }
        public PluginParameterForm(TrayPlugin tp)
        {
            InitializeComponent();
            this.tp = tp;
            LoadParameters();
        }
        public void LoadParameters()
        {
            for(int i = 0; i < tp.parameterCount; i++) {
                TrayPluginParameter tpp = null;
                if (i < tp.parameters.Count) {
                    tpp = tp.parameters[i];
                } else {
                    tpp = new TrayPluginParameter();
                    tp.parameters.Add(tpp);
                }
                if (tpp.name != "") {
                    parameterComboBox.Items.Add(tpp.name);
                } else {
                    parameterComboBox.Items.Add(String.Format("Parameter {0}", i + 1));
                }
                parameterComboBox.SelectedIndex = i;
            }
            parameterComboBox.SelectedIndex = 0;
            LoadSelected();
        }
        public void LoadSelected() {
            nameTextBox.Text = tpp.name;
            prefixTextBox.Text = tpp.prefix;
            isBooleanCheckBox.Checked = tpp.isBoolean;
        }
        private void parameterComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadSelected();
        }
        private void nameTextBox_TextChanged(object sender, EventArgs e) {
            tpp.name = nameTextBox.Text;
            if (tpp.name != "") {
                parameterComboBox.Items[parameterComboBox.SelectedIndex] = tpp.name;
            } else {
                parameterComboBox.Items[parameterComboBox.SelectedIndex] = String.Format("Parameter {0}", parameterComboBox.SelectedIndex + 1);
            }
        }
        private void prefixTextBox_TextChanged(object sender, EventArgs e) {
            tpp.prefix = prefixTextBox.Text;
        }
        private void isBooleanCheckBox_CheckedChanged(object sender, EventArgs e) {
            tpp.isBoolean = isBooleanCheckBox.Checked;
        }
    }
}
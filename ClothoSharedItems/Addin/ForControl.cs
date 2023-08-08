using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml.Linq;

namespace ClothoSharedItems
{
    public static class ForControl
    {
        public static void ListUp<T>(this ComboBox target, IEnumerable<T> list, int defaultIndex = 0)
        {
            target.Items.Clear();
            foreach (var item in list) target.Items.Add(item);
            if (target.Items.Count > 0 && defaultIndex >= 0)
                target.SelectedIndex = defaultIndex.Trim(0, target.Items.Count - 1);
        }

        public static void ListUp<T>(this ComboBox target, IEnumerable<T> list, Predicate<T> predicate)
        {
            target.Items.Clear();

            int defaultIndex = -1;
            foreach (var item in list)
            {
                target.Items.Add(item);
                if (defaultIndex < 0 && predicate(item))
                    defaultIndex = target.Items.Count - 1;
            }
            if (defaultIndex >= 0)
                target.SelectedIndex = defaultIndex;
        }

        public static void ListUp(this ComboBox target, Type enumType, int defaultIndex = 0)
        {
            target.Items.Clear();
            target.Items.AddRange(Enum.GetValues(enumType).Cast<object>().ToArray());
            target.SelectedIndex = defaultIndex.Trim(0, target.Items.Count - 1);
        }

        public static void ListUp<T>(this ListBox target, IEnumerable<T> list, int defaultIndex = 0)
        {
            target.Items.Clear();
            foreach (var item in list) target.Items.Add(item);
            if (target.Items.Count > 0 && defaultIndex >= 0)
                target.SelectedIndex = defaultIndex.Trim(0, target.Items.Count - 1);
        }

        public static void ListUp(this ListBox target, Type enumType, int defaultIndex = 0)
        {
            target.Items.Clear();
            target.Items.AddRange(Enum.GetValues(enumType).Cast<object>().ToArray());
        }

        public static void SetValue(this NumericUpDown target, double value)
        {
            target.Value = (decimal)value.Trim((double)target.Minimum, (double)target.Maximum);
        }

        public static void SetRange(this NumericUpDown target, double min, double max)
        {
            //target.Minimum = decimal.MinValue;
            //target.Maximum = decimal.MaxValue;

            //if ((double)target.Value > max)
            //    target.Value = (decimal)max;
            //else if ((double)target.Value < min)
            //    target.Value = (decimal)min;

            target.Minimum = (decimal)min;
            target.Maximum = (decimal)max;
        }

        public static void SetRange(this NumericUpDown target, double min, double max, double defaultValue)
        {
            //target.Minimum = decimal.MinValue;
            //target.Maximum = decimal.MaxValue;

            //target.Value = (decimal)(defaultValue.Trim(min, max));
            //target.Minimum = (decimal)min;
            //target.Maximum = (decimal)max;

            target.Minimum = (decimal)min;
            target.Maximum = (decimal)max;
            target.Value = (decimal)(defaultValue.Trim(min, max));
        }

        public static XElement GetXML(this ComboBox comboBox)
        {
            var xmlItem = getBaseXML(comboBox);
            if (comboBox.SelectedItem != null)
                xmlItem.SetValue(comboBox.SelectedItem);
            else xmlItem.SetValue(string.Empty);
            return xmlItem;
        }

        public static XElement GetXML(this TextBox textBox)
        {
            var xmlItem = getBaseXML(textBox);
            xmlItem.SetValue(textBox.Text);
            return xmlItem;
        }

        public static XElement GetXML(this CheckBox checkBox)
        {
            var xmlItem = getBaseXML(checkBox);
            xmlItem.SetValue(checkBox.CheckState);
            return xmlItem;
        }

        public static XElement GetXML(this RadioButton radioButton)
        {
            var xmlItem = getBaseXML(radioButton);
            xmlItem.SetValue(radioButton.Checked);
            return xmlItem;
        }

        public static XElement GetXML(this NumericUpDown numericUpDown)
        {
            var xmlItem = getBaseXML(numericUpDown);
            xmlItem.SetValue(numericUpDown.Value);
            return xmlItem;
        }

        public static XElement GetXML(this TabControl tabControl)
        {
            var xmlItem = getBaseXML(tabControl);
            xmlItem.SetValue(tabControl.SelectedTab.Name);
            return xmlItem;
        }

        public static Control[] GetSubordinates(this Control superior)
        {
            var subordiates = new List<Control>();
            populateSubordinates(superior, subordiates);
            return subordiates.ToArray();
        }

        private static XElement getBaseXML(Control control)
        {
            var xmlItem = new XElement("Control");
            xmlItem.SetAttributeValue("Type", control.GetType().Name);
            xmlItem.SetAttributeValue("Name", control.Name);
            return xmlItem;
        }

        private static void populateSubordinates(Control superior, List<Control> subordinates)
        {
            foreach (var ctrlSub in superior.Controls.OfType<Control>().OrderBy(c => c.TabIndex))
            {
                if (ctrlSub.Name.StartsWith("_")) continue;

                if ((ctrlSub is ComboBox) || (ctrlSub is TextBox) || (ctrlSub is CheckBox) || (ctrlSub is NumericUpDown) || (ctrlSub is RadioButton))
                    subordinates.Add(ctrlSub);
                else if (ctrlSub is TabControl)
                {
                    subordinates.Add(ctrlSub);
                    foreach (TabPage ctrlPage in ((TabControl)ctrlSub).TabPages)
                        populateSubordinates(ctrlPage, subordinates);
                }
                else if (ctrlSub.Controls.Count > 0)
                {
                    populateSubordinates(ctrlSub, subordinates);
                }
            }
        }
    }

    public static class SystemMenu
    {
        private const int MF_STRING = 0x0;
        private const int MF_SEPARATOR = 0x800;

        // P/Invoke declarations
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool AppendMenu(IntPtr hMenu, int uFlags, int uIDNewItem, string lpNewItem);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool InsertMenu(IntPtr hMenu, int uPosition, int uFlags, int uIDNewItem, string lpNewItem);

        public const int WM_SYSCOMMAND = 0x112;
        public const int SYSMENU_ID_SEPARATOR = 0;
        public const int SYSMENU_ID_OPEN_EXEDIR = 0x1;
        public const int SYSMENU_ID_OPEN_CFGFILE = 0x2;
        public const int SYSMENU_ID_RELOAD_CFGFILE = 0x3;
        public const int SYSMENU_ID_UPDATE_DEVICES = 0x4;
        public const int SYSMENU_ID_RESET_PATHLOSS = 0x5;
        public const int SYSMENU_ID_SHOWDLG_LOGGER = 0x21;

        public static void AddSystemMenu(IntPtr hWnd, params int[] menuIDs)
        {
            var hSysMenu = GetSystemMenu(hWnd, false);

            foreach (var id in menuIDs)
            {
                if (id == SYSMENU_ID_SEPARATOR)
                    AppendMenu(hSysMenu, MF_SEPARATOR, 0, string.Empty);
                else if (id == SYSMENU_ID_OPEN_EXEDIR)
                    AppendMenu(hSysMenu, MF_STRING, SYSMENU_ID_OPEN_EXEDIR, "실행폴더 열기");
                else if (id == SYSMENU_ID_OPEN_CFGFILE)
                    AppendMenu(hSysMenu, MF_STRING, SYSMENU_ID_OPEN_CFGFILE, "환경파일 열기");
                else if (id == SYSMENU_ID_RELOAD_CFGFILE)
                    AppendMenu(hSysMenu, MF_STRING, SYSMENU_ID_RELOAD_CFGFILE, "환경파일 다시 불러오기");
                else if (id == SYSMENU_ID_UPDATE_DEVICES)
                    AppendMenu(hSysMenu, MF_STRING, SYSMENU_ID_UPDATE_DEVICES, "장비 주소 맞추기");
                else if (id == SYSMENU_ID_RESET_PATHLOSS)
                    AppendMenu(hSysMenu, MF_STRING, SYSMENU_ID_RESET_PATHLOSS, "Loss 변경하기");
                else if (id == SYSMENU_ID_SHOWDLG_LOGGER)
                    AppendMenu(hSysMenu, MF_STRING, SYSMENU_ID_SHOWDLG_LOGGER, "Logger 실행하기");
            }
        }
    }

    public static class ControlAddin
    {
        public static void DoubleBuffered(this Control control, bool setting = true)
        {
            Type dgvType = control.GetType();
            PropertyInfo pi = dgvType.GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            pi.SetValue(control, setting, null);
        }
    }
}
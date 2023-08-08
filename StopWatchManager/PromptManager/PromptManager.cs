using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace MPAD_TestTimer
{
    /// <summary>
    /// Centralized dialog prompting.
    /// </summary>
    public class PromptManager
    {
        private static PromptManager instance;

        public static PromptManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new PromptManager();
                }

                return instance;
            }
        }

        public bool IsAutoAnswer { get; set; }

        public void ShowError(string message)
        {
            MessageBox.Show(message, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        public void ShowError(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public void ShowError(string message1, string message2, string title)
        {
            string msg = String.Format("{1}{0}{0}{2}",
                Environment.NewLine, message1, message2);
            MessageBox.Show(msg, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public void ShowError(Exception ex)
        {
            if (ex == null) return;
            MessageBox.Show(ex.ToString());
        }

        public void ShowError(string message, Exception ex)
        {
            string msg = String.Format("{1}{0}{0}{2}",
                Environment.NewLine, message, ex);
            MessageBox.Show(msg);
        }

        public void ShowError(ValidationDataObject vdo)
        {
            if (vdo.IsValidated) return;
            string msg = String.Format("{1}{0}{0}{2}",
                Environment.NewLine, vdo.ErrorMessage, vdo.Exception);
            MessageBox.Show(msg, vdo.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public DialogResult ShowDialogYesNoCancel(string message, string title)
        {
            if (IsAutoAnswer)
            {
                return DialogResult.No;
            }

            DialogResult dr = MessageBox.Show(message, title, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            return dr;
        }

        public DialogResult ShowDialogRetryCancel(string message, string title)
        {
            DialogResult dr = MessageBox.Show(message, title, MessageBoxButtons.RetryCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            return dr;
        }

        public DialogResult ShowDialogOKCancel(string message, string title)
        {
            DialogResult dr = MessageBox.Show(message, title, MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            return dr;
        }

        public string ShowMultiSelectionDialog(string textBox1text, string dialogTitle,
            Dictionary<string, string> selectionList, string defaultSelectedKeyName)
        {
            if (IsAutoAnswer)
            {
                return defaultSelectedKeyName;
            }

            MultipleSelectionDialog dialog = new MultipleSelectionDialog();
            dialog.SetMessage(textBox1text, dialogTitle, selectionList, defaultSelectedKeyName);
            dialog.ShowDialog();
            return dialog.SelectedButton;
        }

        public string ShowTextInputDialog(string messageLine1, string messageLine2,
            string dialogTitle, string defaultInput)
        {
            TextInputDialog dialog = new TextInputDialog();
            dialog.SetMessage(messageLine1, messageLine2, dialogTitle, defaultInput);
            dialog.ShowDialog();
            return dialog.InputText;
        }

        public void ShowInfo(string message)
        {
            MessageBox.Show(message);
        }

        public void ShowInfo(string message, string title)
        {
            MessageBox.Show(message, title);
        }
    }
}

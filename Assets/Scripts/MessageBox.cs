using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class MessageBox : MonoBehaviour
{
    private class MessagePackage
    {
        public string Caption;
        public string Message;
        public MessageBoxIcon Icon;
        public MessageBoxButton Button;
        public Action Yes;
        public Action No;
        public Action Cancel;
    }

    public Text Caption;
    public Text Message;
    public Image Icon;
    public Button Yes;
    public Button No;
    public Button Cancel;

    private Queue<MessagePackage> packages = new Queue<MessagePackage>();
    private MessagePackage current;

    void Awake()
    {
        Yes.onClick.AddListener(OnYes);
        No.onClick.AddListener(OnNo);
        Cancel.onClick.AddListener(OnCancel);
        //App.Hide(this);
    }

    public void Show(string message)
    {
        Show("", message);
    }

    public void Show(string caption, string message)
    {
        Show(caption, message, MessageBoxButton.Yes, MessageBoxIcon.None, null, null, null);
    }
    public void Show(string message, Action yes)
    {
        Show("", message, yes);
    }

    public void Show(string caption, string message, Action yes)
    {
        Show(caption, message, MessageBoxButton.Yes, MessageBoxIcon.None, yes, null, null);
    }
    
    public void Show(string caption, string message, 
        MessageBoxButton button, MessageBoxIcon icon, 
        Action yes, Action no, Action cancel)
    {
        MessagePackage package = new MessagePackage();
        package.Message = message;
        package.Caption = caption;
        package.Button = button;
        package.Icon = icon;
        package.Yes = yes;
        package.No = no;
        package.Cancel = cancel;
        Enqueue(package);
        if (current == null) Dequeue();
    }

    private void Enqueue(MessagePackage package)
    {
        packages.Enqueue(package);
    }

    private void Dequeue()
    {
        if (packages.Count() == 0)
        {
            current = null;
            App.Hide(this);
            return;
        }
        current = packages.Dequeue();
        Caption.text = current.Caption;
        Message.text = current.Message;
        if ((current.Button & MessageBoxButton.Yes) != MessageBoxButton.None) App.Show(Yes); else App.Hide(Yes);
        if ((current.Button & MessageBoxButton.No) != MessageBoxButton.None) App.Show(No); else App.Hide(No);
        if ((current.Button & MessageBoxButton.Cancel) != MessageBoxButton.None) App.Show(Cancel); else App.Hide(Cancel);
        App.Show(this);
    }

    private void OnYes()
    {
        current?.Yes?.Invoke();
        Dequeue();
    }

    private void OnNo()
    {
        current?.No?.Invoke();
        Dequeue();
    }

    private void OnCancel()
    {
        current?.Cancel?.Invoke();
        Dequeue();

    }
}

public enum MessageBoxButton
{
    None = 0,
    Yes = 1,
    No = 2,
    Cancel = 4,
    YesNo = Yes | No,
    YesNoCancel = Yes | No | Cancel,
}

public enum MessageBoxIcon
{
    None = 0,
    Information = 1,
    Error = 2,
    Question = 3,
}

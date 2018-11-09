using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace GenereateUpdateXML
{
    public partial class Form1 : DevComponents.DotNetBar.Metro.MetroForm
    {
        private static string project = Directory.GetParent(Path.Combine(Directory.GetCurrentDirectory(), @"..\..")).Name;
        private static string xml_dir = Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..");
        private static string xml_path = Path.Combine(xml_dir, project + @"_Update.xml");
        private static string exe_path = Path.Combine(xml_dir, project + @"\bin\Release\" + project + ".exe");
        private static string exe_name = Path.GetFileName(exe_path);
        private static string exe_name_no_ext = Path.GetFileNameWithoutExtension(exe_path);
        private static string exe_URL = "https://raw.githubusercontent.com/henryxrl/" + project + "/master/" + project + "/bin/Release/" + project + ".exe";
        private static string exe_MD5 = GetMd5(exe_path);
        private static string exe_ver = FileVersionInfo.GetVersionInfo(exe_path).ProductVersion;

        private XmlDocument doc = new XmlDocument();
        private XmlNode root;
        private XmlNode update;

        public Form1()
        {
            InitializeComponent();

            // DPI settings
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            root = doc.CreateElement("sharpUpdate");
            doc.AppendChild(root);

            update = doc.CreateElement("update");
            XmlAttribute attribute = doc.CreateAttribute("appID");
            attribute.Value = exe_name_no_ext;
            update.Attributes.Append(attribute);
            root.AppendChild(update);

            appendNode("version", exe_ver);
            appendNode("url", exe_URL);
            appendNode("fileName", exe_name);
            appendNode("md5", exe_MD5);
            appendNode("launchArgs", "");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string log = "";
            string log_en = "";
            string log_cn = "";

            var log_en_array = textBox1.Lines;
            if (log_en_array.Length == 0)
                log_en = "";
            else
                log_en = string.Join("\n    ", log_en_array);

            var log_cn_array = textBox2.Lines;
            if (log_cn_array.Length == 0)
                log_cn = "";
            else
                log_cn = string.Join("\n    ", log_cn_array);

            string date = DateTime.Today.ToString("d");

            log = string.Concat(date, " Update:\n    ", log_en, "\n\n", date, " 更新：\n    ", log_cn);

            appendNode("description", log);
            doc.Save(xml_path);

            Environment.Exit(1);
        }

        static private string GetMd5(string path)
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            FileStream stream = File.Open(path, FileMode.Open);
            md5.ComputeHash(stream);
            stream.Close();

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < md5.Hash.Length; i++)
            {
                sb.Append(md5.Hash[i].ToString("x2"));
            }

            return sb.ToString().ToUpper();
        }

        private void appendNode(string nodeName, string value)
        {
            XmlNode version = doc.CreateElement(nodeName);
            version.InnerText = value;
            update.AppendChild(version);
        }
    }
}

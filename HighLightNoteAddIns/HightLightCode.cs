﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Extensibility;
using Microsoft.Office.Interop.OneNote;
using Microsoft.Office.Core;
using System.Windows.Forms;
using System.Runtime.InteropServices.ComTypes;
using System.Drawing.Imaging;
using System.IO;
using System.Xml.Linq;
using System.Diagnostics;
using System.Reflection;
using HighLightBuild;
using HighLightForm;

namespace HighLightNoteAddIns
{



    //{B2727A93-9C8E-412B-B6E6-4C836B358AFF}
    [ComVisible(true)]
    [Guid("D5ECCD00-CF2D-409B-B65A-BDBACB9F21DB"), ProgId("HighLightNote")]
    public class HighLightCode : IDTExtensibility2, IRibbonExtensibility
    {

        private XNamespace _ns;
        private CodeInputForm _codeForm;

        Microsoft.Office.Interop.OneNote.Application onApp = new Microsoft.Office.Interop.OneNote.Application();
        //private object application;
        public string GetCustomUI(string RibbonID)
        {
            return Properties.Resources.ribbon;
        }

        public void OnAddInsUpdate(ref Array custom)
        {

        }

        public void OnBeginShutdown(ref Array custom)
        {
            //if (application != null)
            //    application = null;
            if (onApp != null)
                onApp = null;
        }

        public void OnConnection(object Application, ext_ConnectMode ConnectMode, object AddInInst, ref Array custom)
        {
            //application = Application;
            onApp = (Microsoft.Office.Interop.OneNote.Application)Application;
        }

        public void OnDisconnection(ext_DisconnectMode RemoveMode, ref Array custom)
        {
            //application = null;
            onApp = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public void OnStartupComplete(ref Array custom)
        {

        }

        /// <summary>
        /// 插件入口
        /// </summary>
        /// <param name="control"></param>
        public void onStart(IRibbonControl control)
        {
            string fileName = Guid.NewGuid().ToString();

            //调用HighLightForm程序，显示用户输入窗口，产生代码渲染后的html文件
            //try
            //{
            //    ProcessStartInfo info = new ProcessStartInfo();
            //    info.WorkingDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));


            //    info.FileName = "HighLightForm.exe";
            //    info.Arguments = " " + fileName;
            //    info.WindowStyle = ProcessWindowStyle.Normal;

            //    Process p = new Process();
            //    p.StartInfo = info;
            //    p.Start();
            //    p.WaitForInputIdle();
            //    if (!p.HasExited)
            //        p.WaitForExit();
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("执行HighLightForm.exe出错：" + ex.Message);
            //    return;
            //}
            //启动窗口程序
            string workDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            _codeForm = new CodeInputForm(fileName,workDirectory);
            System.Windows.Forms.Application.Run(_codeForm);

            string outFileName = Path.Combine(Path.GetTempPath(), fileName + ".html");

            if (File.Exists(outFileName))
                insertCodeToCurrentSide(outFileName);

        }
        public IStream GetImage(string imageName)
        {
            MemoryStream mem = new MemoryStream();
            Properties.Resources.Logo.Save(mem, ImageFormat.Png);
            return new CCOMStreamWrapper(mem);
        }

        /// <summary>
        /// 插入代码到当前光标位置
        /// </summary>
        /// <param name="fileName">代码渲染后的文件位置</param>
        private void insertCodeToCurrentSide(string fileName)
        {
            string noteBookXml;
            onApp.GetHierarchy(null, HierarchyScope.hsPages, out noteBookXml);

            var doc = XDocument.Parse(noteBookXml);
            _ns = doc.Root.Name.Namespace;//获取OneNote XML文件的命名空间

            var pageNode = doc.Descendants(_ns + "Page")
                .Where(n => n.Attribute("isCurrentlyViewed") != null && n.Attribute("isCurrentlyViewed").Value == "true")
                .FirstOrDefault();

            string SelectedPageID = pageNode.Attribute("ID").Value;

            string SelectedPageContent;
            //获取当前页面的XML内容
            onApp.GetPageContent(SelectedPageID, out SelectedPageContent, PageInfo.piSelection);
            var SelectedPageXml = XDocument.Parse(SelectedPageContent);

            pageNode = SelectedPageXml.Descendants(_ns + "Page").FirstOrDefault();
            XElement pointNow = pageNode
                .Descendants(_ns + "T").Where(n => n.Attribute("selected") != null && n.Attribute("selected").Value == "all")
                .First();
            if (pointNow != null)
            {

                var isTitle = pointNow.Ancestors(_ns + "Title").FirstOrDefault();

                if (isTitle != null)
                {
                    MessageBox.Show("代码不能插入标题中");
                    return;
                }

            }
            else
            {
                return;
            }

            try
            {
                //将html内容转化为XML内容
                //更新当前页面的XML内容
                XmlBuild builder = new XmlBuild(fileName, _ns);
                builder.XmlReBuilding(ref pageNode, ref pointNow);

                onApp.UpdatePageContent(pageNode.ToString(), DateTime.MinValue);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

        }

    }
}

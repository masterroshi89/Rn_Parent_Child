using System.AddIn;
using System.Drawing;
using System.Windows.Forms;
using RightNow.AddIns.AddInViews;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System;
using Newtonsoft.Json.Linq;


namespace ParentChildChecker
{
    public class PcChecker : Panel, IWorkspaceComponent2
    {
        private IRecordContext _recordContext;
        private IGlobalContext _globalContext;
        private IIncident _incident;
        private string _OracleBaseUrl;
        bool IsCanceled = false;

        public PcChecker(bool inDesignMode, IRecordContext RecordContext,IGlobalContext GlobalContext)
        {
           if(!DesignMode)
            {
                _globalContext = GlobalContext;
                _recordContext = RecordContext;

            }

           if(_recordContext!=null)
            {
                _recordContext.Saving += _recordContext_Saving;             
            }
            
        }

        void _recordContext_Saving(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _recordContext.TriggerNamedEvent("OnSaving");
            if (IsCanceled == true)
            {
                e.Cancel = true;
                IsCanceled = false;
            }
        }

        public class Link
        {
            public string rel { get; set; }
            public string href { get; set; }
            public string mediaType { get; set; }
        }

        public class QueryResponse
        {
            public int count { get; set; }
            public string name { get; set; }
            public IList<string> columnNames { get; set; }
            public IList<IList<string>> rows { get; set; }
            public IList<Link> links { get; set; }
        }


        void getrecords()
        {
            var responseValue = string.Empty;
            try
            {
                _OracleBaseUrl = "https://coeinterview-1.custhelp.com/services/rest/connect/v1.3/analyticsReportResults";
                _incident = _recordContext.GetWorkspaceRecord(RightNow.AddIns.Common.WorkspaceRecordType.Incident) as IIncident;
                var incidentID = _incident.ID; 
                var ReportID = 100005;   
                var payload = "{\"id\":" + ReportID +",\"filters\":{\"name\":\"RefNumber\",\"values\":\""+ incidentID + "\"}}";

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_OracleBaseUrl);
                request.Method = "POST";
                request.Timeout = 30000;
                request.ContentType = "application/json";
                request.ContentLength = payload.Length;
                request.Headers.Add("Authorization", "Basic Y29lX2d1cnByZWV0OlNyaXNodGlAMjAxOQ==");
                using (Stream webStream = request.GetRequestStream())
                using (StreamWriter requestWriter = new StreamWriter(webStream, System.Text.Encoding.ASCII))
                {
                    requestWriter.Write(payload);
                }

                try
                {
                    WebResponse webResponse = request.GetResponse();
                    using (Stream webStream = webResponse.GetResponseStream() ?? Stream.Null)
                    using (StreamReader responseReader = new StreamReader(webStream))
                    {
                        responseValue = responseReader.ReadToEnd();
                        Console.Out.WriteLine(responseValue);
                        var count = (JObject.Parse(responseValue)).First;
                        if (Convert.ToInt32(count.ToString().Split(':')[1]) > 0)
                        {
                            string message = "Do you want to Close the parent Incident,If Clicked Yes Child Incidents will get close with this incident, Please refer to Related Incident Tab for Open Child Incidents";
                            string title = "Incident Alert";
                            MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                            DialogResult result = MessageBox.Show(message, title, buttons, MessageBoxIcon.Warning);
                            if (result == DialogResult.No)
                            {
                                IsCanceled = true;
                            }
                        }                                             
                    }
                }

                catch (Exception ex)
                {
                    Console.Out.WriteLine("-----------------");
                    Console.Out.WriteLine(ex.Message);
                }

            }
            catch (Exception ex)
            {
                Console.Out.WriteLine("-----------------");
                Console.Out.WriteLine(ex.Message);
            }
        }

        #region IAddInControl Members

        public Control GetControl()
        {
            return this;
        }

        #endregion

        #region IWorkspaceComponent2 Members
        public bool ReadOnly { get; set; }

        public void RuleActionInvoked(string ActionName)
        {
            if (ActionName.Equals("MasterIncidentClosure"))
            {
                try
                {
                    getrecords();
                }
                catch (Exception ex)
                {
                    Console.Out.WriteLine("-----------------");
                    Console.Out.WriteLine(ex.Message);
                }
                
            }

        }

        public string RuleConditionInvoked(string ConditionName)
        {
            return string.Empty;
        }

        #endregion
    }

    [AddIn("Workspace Factory AddIn", Version = "1.0.0.0")]
    public class WorkspaceAddInFactory : IWorkspaceComponentFactory2
    {
        #region IWorkspaceComponentFactory2 Members
        private IGlobalContext _globalContext;

        /// <summary>
        /// Method which is invoked by the AddIn framework when the control is created.
        /// </summary>
        /// <param name="inDesignMode">Flag which indicates if the control is being drawn on the Workspace Designer. (Use this flag to determine if code should perform any logic on the workspace record)</param>
        /// <param name="RecordContext">The current workspace record context.</param>
        /// <returns>The control which implements the IWorkspaceComponent2 interface.</returns>
        public IWorkspaceComponent2 CreateControl(bool inDesignMode, IRecordContext RecordContext)
        {
            return new PcChecker(inDesignMode, RecordContext,_globalContext);
        }

        #endregion

        #region IFactoryBase Members

        /// <summary>
        /// The 16x16 pixel icon to represent the Add-In in the Ribbon of the Workspace Designer.
        /// </summary>
        public Image Image16
        {
            get { return Properties.Resources.AddIn16; }
        }

        /// <summary>
        /// The text to represent the Add-In in the Ribbon of the Workspace Designer.
        /// </summary>
        public string Text
        {
            get { return "WorkspaceAddIn"; }
        }

        /// <summary>
        /// The tooltip displayed when hovering over the Add-In in the Ribbon of the Workspace Designer.
        /// </summary>
        public string Tooltip
        {
            get { return "WorkspaceAddIn Tooltip"; }
        }

        #endregion

        #region IAddInBase Members

        /// <summary>
        /// Method which is invoked from the Add-In framework and is used to programmatically control whether to load the Add-In.
        /// </summary>
        /// <param name="GlobalContext">The Global Context for the Add-In framework.</param>
        /// <returns>If true the Add-In to be loaded, if false the Add-In will not be loaded.</returns>
        public bool Initialize(IGlobalContext GlobalContext)
        {
            _globalContext = GlobalContext;
            return true;
        }

        #endregion
    }
}
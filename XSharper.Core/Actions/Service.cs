#region -- Copyrights --
// ***********************************************************************
//  This file is a part of XSharper (http://xsharper.com)
// 
//  Copyright (C) 2006 - 2010, Alexei Shamov, DeltaX Inc.
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//  
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
//  
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
// ************************************************************************
#endregion
using System;
using System.ComponentModel;
using System.ServiceProcess;

namespace XSharper.Core
{
    /// <summary>
    /// Types of service requests
    /// </summary>
    public enum ServiceCommand
    {
        /// Default value, no action
        [Description("Default value, no action")]
        None,

        /// Stop service
        [Description("Stop service")]
        Stop,

        /// Start service
        [Description("Start service")]
        Start,

        /// Start service
        [Description("Restart service")]
        Restart,

        /// Pause service
        [Description("Pause service")]
        Pause,

        /// Wait until service is stopped
        [Description("Wait until service is stopped")]
        WaitForStopped,

        /// Wait until service is in running state
        [Description("Wait until service is in running state")]
        WaitForRunning,

        /// Get service status
        [Description("Get service status")]
        Status,

        /// Output true if service is installed, or false otherwise
        [Description("Output true if service is installed, or false otherwise")]
        IsInstalled
    }

    /// <summary>
    /// Service control command
    /// </summary>
    [XsType("service", ScriptActionBase.XSharperNamespace)]
    [Description("Service control command")]
    public class Service : ScriptActionBase
    {
        /// Name of the service
        [Description("Name of the service")]
        [XsRequired]
        [XsAttribute("name")]
        [XsAttribute("serviceName",Deprecated = true)]
        public string ServiceName { get; set; }

        /// Computer name where service is running. Default is '.' = current computer.
        [Description("Computer name where service is running. Default is '.' = current computer.")]
        [XsAttribute("machine")]
        [XsAttribute("machineName", Deprecated = true)]
        public string MachineName { get; set; }

        /// Service command to execute, required
        [Description("Service command to execute")]
        public ServiceCommand Command { get; set; }

        /// Timeout. Default 30 seconds
        [Description("Timeout. Default 30 seconds")]
        public string Timeout { get; set; }

        /// Where to output result of the action
        [Description("Where to output result of the action")]
        public string OutTo { get; set; }

        /// Constructor
        public Service()
        {
            MachineName = ".";
            Timeout = "00:00:30";
        }
        /// Execute action
        public override object Execute()
        {
            string svcName = Context.TransformStr(ServiceName, Transform);
            string machineName = Context.TransformStr(MachineName, Transform);
            if (Command==ServiceCommand.IsInstalled)
            {
                bool installed = false;
                foreach (ServiceController svc in ServiceController.GetServices(machineName))
                {
                    if (string.Compare(svc.ServiceName, svcName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        installed = true;
                        break;
                    }
                }
                Context.OutTo(Context.TransformStr(OutTo, Transform), installed);
                return null;
            }

            using (var sc = new ServiceController(svcName,machineName))
            {
                switch (Command)
                {
                    case ServiceCommand.Stop:
                        if (sc.Status!=ServiceControllerStatus.Stopped)
                            sc.Stop();
                        break;
                    case ServiceCommand.Start:
                        if (sc.Status != ServiceControllerStatus.Running)
                            sc.Start();
                        break;
                    case ServiceCommand.Restart:
                        if (sc.Status != ServiceControllerStatus.Stopped)
                            sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, Utils.ToTimeSpan(Timeout).Value);
                        sc.Start();
                        break;
                    case ServiceCommand.Pause:
                        sc.Pause();
                        break;
                    case ServiceCommand.WaitForRunning:
                        sc.WaitForStatus(ServiceControllerStatus.Running,Utils.ToTimeSpan(Timeout).Value);
                        break;
                    case ServiceCommand.WaitForStopped:
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, Utils.ToTimeSpan(Timeout).Value);
                        break;
                    case ServiceCommand.Status:
                        Context.OutTo(Context.TransformStr(OutTo, Transform), sc.Status);
                        break;
                    default:
                        throw new ParsingException("Unknown action: " + Command);
                }
            }
            return null;
        }
    }
}
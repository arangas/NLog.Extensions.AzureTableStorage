﻿using System;
using System.Collections;
using System.Net.NetworkInformation;
using System.Text;
using Microsoft.WindowsAzure.Storage.Table;

namespace NLog.Extensions.AzureTableStorage
{
    /// <summary>
    /// This is the entity class that represents the records being written out to Azure Storage
    /// </summary>
    public class LogEntity : TableEntity
    {
        #region Constants
        #endregion Constants

        #region Fields
        private readonly object _syncRoot = new object();
        #endregion Fields

        #region Properties
        public string LogTimeStamp { get; set; }

        public string Level { get; set; }

        public string LoggerName { get; set; }

        public string Message { get; set; }

        public string Exception { get; set; }

        public string InnerException { get; set; }

        public string StackTrace { get; set; }

        public string MessageWithLayout { get; set; }

        public string ExceptionData { get; set; }

        public string MachineName { get; set; }
        #endregion Properties

        #region Constructors
        public LogEntity()
        {
        }

        public LogEntity(string partitionKey, string rowKey, LogEventInfo logEvent, string layoutMessage)
        {
            lock (_syncRoot)
            {
                LoggerName = logEvent.LoggerName;
                LogTimeStamp = logEvent.TimeStamp.ToString("g");
                Level = logEvent.Level.Name;
                Message = logEvent.FormattedMessage;
                MessageWithLayout = layoutMessage;

                if (logEvent.Exception != null)
                {
                    Exception = logEvent.Exception.ToString();
                    if (logEvent.Exception.Data.Count > 0)
                    {
                        ExceptionData = GetExceptionDataAsString(logEvent.Exception);
                    }
                    if (logEvent.Exception.InnerException != null)
                    {
                        InnerException = logEvent.Exception.InnerException.ToString();
                    }
                }

                if (logEvent.StackTrace != null)
                {
                    StackTrace = logEvent.StackTrace.ToString();
                }

                PartitionKey = TransformKey(partitionKey, logEvent);
                RowKey = TransformKey(rowKey, logEvent);

                MachineName = Environment.MachineName;
            }
        }
        #endregion Constructors

        #region Methods
        private static string TransformKey(string key, LogEventInfo logEvent)
        {
            var date = logEvent.TimeStamp.ToUniversalTime();

            return key
                .Replace("${date}", date.ToString("yyyyMMdd"))
                .Replace("${time}", date.ToString("HHmmss"))
                .Replace("${ticks}", date.Ticks.ToString("d19"))
                .Replace("${longdate}", date.ToString("yyyyMMddHHmmssffffff"))
                .Replace("${micros}", date.ToString("ffffff"))
                .Replace("${guid}", Guid.NewGuid().ToString("N"))
                .Replace("${logger}", logEvent.LoggerName)
                .Replace("${level}", logEvent.Level.ToString())
                .Replace("${level:uppercase=true}", logEvent.Level.ToString().ToUpper())
                .Replace("${machine}", Environment.MachineName)
                .Replace("${descticks}", (DateTime.MaxValue.Ticks - date.Ticks).ToString("d19"));
        }

        private static string GetExceptionDataAsString(Exception exception)
        {
            var data = new StringBuilder();
            foreach (DictionaryEntry entry in exception.Data)
            {
                data.AppendLine(entry.Key + "=" + entry.Value);
            }
            return data.ToString();
        }
        #endregion Methods
    }
}

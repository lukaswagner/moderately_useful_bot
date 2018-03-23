﻿using System;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;
using System.Xml;

namespace ModeratelyUsefulBot
{
    internal static class Config
    {
        private static string _tag = "Config";
#if DEBUG
        private static string _file = "data/debugConfig.xml";
#else
        private static string _file = "data/config.xml";
#endif
        private static XmlDocument _doc = new XmlDocument();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static Config() => _doc.Load(_file);

        public static bool DoesPropertyExist(string property) => _doc.SelectSingleNode("/config/" + property) != null;

        public static bool Get<T>(string property, out T value)
        {
            value = default(T);
            var valueString = "";

            var node = _doc.SelectSingleNode("/config/" + property);

            if (node == null)
            {
                Log.Warn(_tag, "Tried to read property which doesn't exist: " + property);
                return false;
            }

            valueString = node.InnerText;

            try
            {
                value = (T)Convert.ChangeType(valueString, typeof(T), CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception ex)
            {
                if (ex is InvalidCastException
                    || ex is FormatException
                    || ex is OverflowException
                    || ex is ArgumentNullException)
                {
                    Log.Error(_tag, "Could not convert property " + property + " with value " + valueString + " to type " + typeof(T).Name + ".");
                    return false;
                }

                throw;
            }
        }

        public static T GetDefault<T>(string property, T defaultValue)
        {
            if (!DoesPropertyExist(property))
            {
                Log.Info(_tag, "Could not find property " + property + ". Using default value (" + defaultValue.ToString() + ").");
                return defaultValue;
            }
            Get<T>(property, out var outValue);
            return outValue;
        }

        public static bool Set(string property, object value)
        {
            var value_string = value.ToString();

            var node = _traveseOrBuildHierarchy("/config/" + property);

            node.InnerText = value_string;

            try
            {
                File.WriteAllText("Config/config.xml", Beautify(_doc));
            }
            catch (Exception ex)
            {
                if (ex is ArgumentException
                    || ex is ArgumentNullException
                    || ex is PathTooLongException
                    || ex is DirectoryNotFoundException
                    || ex is IOException
                    || ex is UnauthorizedAccessException
                    || ex is NotSupportedException
                    || ex is SecurityException)
                {
                    Log.Error(_tag, "Could not set property " + property + ":\n" + ex.Message);
                    return false;
                }

                throw;
            }

            return true;
        }

        private static XmlNode _traveseOrBuildHierarchy(string path)
        {
            var split = path.Trim('/').Split('/');

            var parent = _doc as XmlNode;

            foreach (var node in split)
            {
                var current = parent.SelectSingleNode(node);
                if (current == null)
                    current = parent.AppendChild(_doc.CreateElement(node));

                parent = current;
            }

            return parent;
        }

        private static string Beautify(XmlDocument doc)
        {
            var stringBuilder = new StringBuilder();
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "    ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace
            };
            using (var writer = XmlWriter.Create(stringBuilder, settings))
            {
                doc.Save(writer);
            }
            return stringBuilder.ToString() + Environment.NewLine;
        }
    }
}

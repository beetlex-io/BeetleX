using BeetleX.Buffers;
using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.HttpExtend
{
    public class Header
    {

        #region header Type

        public const string ACCEPT = "Accept";

        public const string ACCEPT_ENCODING = "Accept-Encoding";

        public const string ACCEPT_LANGUAGE = "Accept-Language";

        public const string CACHE_CONTROL = "Cache-Control";

        public const string CONNECTION = "Connection";

        public const string COOKIE = "Cookie";

        public const string HOST = "Host";

        public const string REFERER = "Referer";

        public const string USER_AGENT = "User-Agent";

        public const string STATUS = "Status";

        public const string CONTENT_TYPE = "Content-Type";

        public const string CONTENT_LENGTH = "Content-Length";

        public const string CONTENT_ENCODING = "Content-Encoding";

        public const string TRANSFER_ENCODING = "Transfer-Encoding";

        public const string SERVER = "Server";


        #endregion

        private System.Collections.Specialized.NameValueCollection mItems = new System.Collections.Specialized.NameValueCollection();

        public System.Collections.Specialized.NameValueCollection Items { get { return mItems; } }

        public void Add(string name, string value)
        {
            mItems[name] = value;
        }

        public void Import(string line)
        {
            if (!string.IsNullOrEmpty(line))
            {
                string[] values = line.Split(':');
                if (values.Length > 1)
                    Add(values[0], values[1].Trim().ToLower());
            }
        }

        public string this[string name]
        {
            get
            {
                return mItems[name];
            }
            set
            {
                mItems[name] = value;
            }
        }

        public bool Read(PipeStream stream)
        {
            string line = null;
            while (stream.TryReadLine(out line))
            {
                if (string.IsNullOrEmpty(line))
                    return true;
                Import(line);
            }
            return false;
        }

        public void Write(PipeStream stream)
        {
            foreach (string key in mItems)
            {
                stream.WriteLine(key + ": " + mItems[key]);
            }
        }
    }


}

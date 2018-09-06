using System;
using System.Collections.Generic;
using System.Text;

namespace Chat
{
    public enum CmdType
    {
        LOGIN,
        SPEAK,
        QUIT,
        OTHER
    }

    public struct Cmd
    {
        public Cmd(CmdType type, string text)
        {
            Type = type;
            Text = text;
        }
        public CmdType Type { get; set; }

        public string Text { get; set; }
    }

    public class ChatParse
    {
        public static string CreateCommand(CmdType cmd, string value)
        {
            switch (cmd)
            {
                case CmdType.LOGIN:
                    return "login:" + value;
                case CmdType.QUIT:
                    return "quit:" + value;
                case CmdType.SPEAK:
                    return "speak:" + value;
            }
            return "other:" + value;
        }
        public static Cmd Parse(string line)
        {
            string[] values = line.Split(':');
            switch (values[0])
            {
                case "login":
                    return new Cmd(CmdType.LOGIN, values[1]);
                case "quit":
                    return new Cmd(CmdType.QUIT, values[1]);
                case "speak":
                    return new Cmd(CmdType.SPEAK, values[1]);
            }
            return new Cmd(CmdType.OTHER, values[1]);
        }


    }
}

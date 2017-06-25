using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UDIMAS;

namespace Discorder
{
    class Discorder : UdimasExternalPlugin
    {
        public override string Name => "Discorder";

        protected override Dictionary<string, dynamic> PluginProperties => 
            new Dictionary<string, dynamic> {
                {"SendMessage",  new Func<string, string, string, string, bool>(SendMessage) }
            };

        public override void Run()
        {
            CmdInterpreter.RegisterCommand(new TerminalCommand("discorder", Terminal));
        }

        private static (int, string) Terminal(InterpreterIOPipeline tw, string[] a)
        {
            bool invalidArgs = false;
            if (CmdInterpreter.IsWellFormatterArguments(a, "-h"))
            {
                tw.WriteLine("Sends a message into a Discord webhook.");
                tw.WriteLine("Usage:");
                tw.WriteLine(" [flags] content");
                tw.WriteLine("Flags: (*required)");
                tw.WriteLine(" *u|url\t\tUrl of the webhook");
                tw.WriteLine(" n|name\t\tName for webhook");
                tw.WriteLine(" a|avatar\tAvatar picture url for the webhook");
            }
            else
            {
                string url = null;
                string name = null;
                string avatar = null;
                string content = null;
                var os = new NDesk.Options.OptionSet()
                {
                    { "u|url=", "", v => url = v },
                    { "n|name=", "", v => name = v },
                    { "a|avatar=", "", v => avatar = v },
                };

                List<string> extra = os.Parse(a);

                if (url == null || 
                    !Uri.IsWellFormedUriString(url, UriKind.Absolute) ||
                    //A (very) clumsy way of making sure the named arguments are on front of argument sequence ('-n myname test' not 'test -n myname')
                    !extra.SequenceEqual(a.Skip(a.Length - extra.Count)))
                    invalidArgs = true; // invalid args
                else
                {
                    content = string.Join(" ", a.Skip(a.Length - extra.Count));
                    SendMessage(url, content, name, avatar);
                }
            }

            if(invalidArgs)
            {
                return (CmdInterpreter.INVALIDARGUMENTS, "Invalid arguments. -h for help");
            }
            return (0, "");
        }

        private static bool SendMessage(string webhookUrl, string content, string username, string avatarUrl)
        {
            if (!Uri.IsWellFormedUriString(webhookUrl, UriKind.Absolute))
            {
                return false;
            }
            using (var client = new HttpClient())
            {
                var values = new Dictionary<string, string> {
                            { "content", content },
                        };

                if (!string.IsNullOrWhiteSpace(username)) values.Add("username", username);
                if (!string.IsNullOrWhiteSpace(avatarUrl)) values.Add("avatar_url", avatarUrl);

                var contentstr = new FormUrlEncodedContent(values);

                try
                {
                    client.PostAsync(webhookUrl, contentstr).Wait();
                }
                catch { return false; }
            }
            return false;
        }
    }
}

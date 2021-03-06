UDIMAS (Utility Device Independent MAnagement System) is a system that has supports for both .NET-made plugins and python script plugins with access to whole .NET framework.
See the wiki for help in installing/configuring UDIMAS.

# Features
 - A simple yet powerful commandline for user-interaction
 - Pretty much no ready made functionality apart from command line so you can tailor it for your own use
 - Allows for method calling from other plugins without dependencies: test if a plugin exists and use it if it does (using [dynamic](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/dynamic) keyword)
 - Allows execution of [Python](https://www.python.org/) scripts (using [IronPython](http://ironpython.net/)). This allows for dynamic module creation/modification without a compiler.

## Ready to use plugins
UDIMAS contains plugins that add possibly useful functionality and that are completely optional to use.

 - UDINet: A plugin that allows UDIMAS instances to find others on a local network and remote command calling (a homemade telnet)
 - Discorder: Allows for simple messages to be send into [Discord](https://discordapp.com/) Webhooks. ([Discord Webhooks](https://support.discordapp.com/hc/en-us/articles/228383668-Intro-to-Webhooks))
 - Documenter: Allows you to "document" (write signatures for classes and everything within) .NET namespaces. You can document EVERYTHING inside .NET.
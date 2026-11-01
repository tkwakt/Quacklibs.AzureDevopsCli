using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quacklibs.AzureDevopsCli.Commands.Release
{
    internal class ReleaseCommand : BaseCommand
    {
        public ReleaseCommand(ReleaseReadCommand releaseReadCommand) : base("release", "read all changes included in an (upcomming) release")
        {
            this.Subcommands.Add(releaseReadCommand);
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Umbraco.Core;

namespace Chauffeur.Deliverables
{
    [DeliverableName("help")]
    [DeliverableAlias("h")]
    [DeliverableAlias("?")]
    public class HelpDeliverable : Deliverable, IProvideDirections
    {
        public HelpDeliverable(TextReader reader, TextWriter writer)
            : base(reader, writer)
        {
        }

        public override async Task<DeliverableResponse> Run(string command, string[] args)
        {
            var deliverables = TypeFinder
                .FindClassesOfType<Deliverable>();

            if (args.Any())
                await Print(deliverables, args[0]);
            else
                await PrintAll(deliverables);

            return DeliverableResponse.Continue;
        }

        private async Task Print(IEnumerable<Type> deliverables, string command)
        {
            var ipd = typeof(IProvideDirections);
            var deliverable = deliverables
                .Where(t => ipd.IsAssignableFrom(t))
                .FirstOrDefault(t => t.GetCustomAttribute<DeliverableNameAttribute>(false).Name == command);

            if (deliverable != null)
            {
                var instance = (IProvideDirections)Activator.CreateInstance(deliverable, new object[] { In, Out });
                await instance.Directions();
                return;
            }

            await Out.WriteLineFormattedAsync(
                "The command '{0}' doesn't implement help, you best contact the author",
                command
            );

        }

        private async Task PrintAll(IEnumerable<Type> deliverables)
        {
            await Out.WriteLineAsync("The following deliverables are loaded. Use `help <deliverable>` for detailed help");

            foreach (var deliverable in deliverables)
            {
                var name = deliverable.GetCustomAttribute<DeliverableNameAttribute>(false).Name;
                var aliases = deliverable.GetCustomAttributes<DeliverableAliasAttribute>(false).Select(a => a.Alias);

                await Out.WriteLineFormattedAsync(
                    "{0}{1}",
                    name,
                    aliases.Any() ? string.Format(" (aliases: {0})", string.Join(", ", aliases)) : string.Empty
                );
            }
        }

        public async Task Directions()
        {
            await Out.WriteLineAsync("help");
            await Out.WriteLineAsync("\taliases: h, ?");
            await Out.WriteLineAsync("\tUse `help` to display system help");
            await Out.WriteLineAsync("\tUse `help <Deliverable>` to display help for a deliverable");
        }
    }
}

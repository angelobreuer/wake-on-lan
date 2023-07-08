using System.CommandLine;
using Wolctl;

await new WolctlCommand().InvokeAsync(args).ConfigureAwait(false);

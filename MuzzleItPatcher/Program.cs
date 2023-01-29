using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;

namespace MuzzleItPatcher
{
	public class Program
	{
		public static async Task<int> Main(string[] args)
		{
			return await SynthesisPipeline.Instance
				.AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
				.SetTypicalOpen(GameRelease.SkyrimSE, "MuzzleItPatch.esp")
				.Run(args);
		}

		public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
		{
			var formLink = new FormLink<IDialogResponsesGetter>(FormKey.Factory("0012C6:MuzzleIt.esp"));
			var exampleDialog = formLink.TryResolve(state.LinkCache);

			if (exampleDialog is null)
			{
				Console.WriteLine("Our source dialog was null!");
				return;
			}

			var conditions = exampleDialog?.Conditions.Select(r => r.DeepCopy()).ToArray();

			if (conditions is null || !conditions.Any())
			{
				Console.WriteLine("Our source condition was null!");
				return;
			}

			foreach (var dialResponse in state.LoadOrder.PriorityOrder.DialogResponses().WinningContextOverrides(state.LinkCache))
			{
				if (!dialResponse.TryGetParent<IDialogTopicGetter>(out var topic))
				{
					continue;
				}

				Console.WriteLine($"{dialResponse.Record.EditorID} => {topic.EditorID} :: {topic.Category} && {topic.Subtype}");

				if (topic is not
					{ Category: DialogTopic.CategoryEnum.Misc, Subtype: DialogTopic.SubtypeEnum.Idle })
				{
					continue;
				}

				var patchedResponse = dialResponse.GetOrAddAsOverride(state.PatchMod);

				patchedResponse.Conditions.InsertRange(conditions, 0);
			}

			if (formLink.TryResolveContext<ISkyrimMod, ISkyrimModGetter, IDialogResponses, IDialogResponsesGetter>(state.LinkCache, out var thing))
			{
				var patched = thing.GetOrAddAsOverride(state.PatchMod);
				patched.SkyrimMajorRecordFlags.SetFlag(SkyrimMajorRecord.SkyrimMajorRecordFlag.Deleted, true);
			}
		}
	}
}
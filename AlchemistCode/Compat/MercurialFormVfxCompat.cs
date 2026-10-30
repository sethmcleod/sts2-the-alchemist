using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;

// COMPAT-BRANCH: main

namespace Alchemist.AlchemistCode.Powers;

// THIS COPY IS THE main (DEFAULT BRANCH) IMPLEMENTATION. ON A MERGE FROM beta, KEEP THIS SIDE.
// The public branch has no form vfx (NFormVfx, FormVfxHolder), so Mercurial Form shows no swirl there
internal static class MercurialFormVfx
{
    public const string TexturePath = $"{MainFile.ResPath}/animations/vfx/mercurial_form/mercurial_form.png";

    public static Node2D? Create(Creature owner) => null;

    public static void OnEffectTriggered(Node2D? vfx)
    {
    }

    public static void FadeOut(Node2D? vfx)
    {
    }
}

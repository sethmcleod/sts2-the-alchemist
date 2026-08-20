using System.Collections.Generic;
using Alchemist.AlchemistCode.Commands;
using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Relics;

public class AuricSeal : AlchemistRelic
{
    private const int Antitoxin = 2;

    // Reset at every side turn start, so the first draw of EVERY turn pays, including turn one's
    // opening hand, whose draws land after the side turn begins
    private bool _paidThisTurn;

    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => Infusion.InfuseTips();

    // BeforeSideTurnStart alone leaves the flag from the previous combat standing until the first
    // side turn begins; a combat-start draw effect would be silently eaten by it
    public override Task BeforeCombatStart()
    {
        _paidThisTurn = false;
        return Task.CompletedTask;
    }

    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (participants.Contains(Owner.Creature)) _paidThisTurn = false;
        return Task.CompletedTask;
    }

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card,
        bool fromHandDraw)
    {
        if (_paidThisTurn || card.Owner != Owner) return;
        _paidThisTurn = true;
        Flash();
        if (Infusion.CanInfuse(card))
        {
            Infusion.Infuse(card);
            CardCmd.Preview(new List<CardModel> { card });
        }
        else
        {
            await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature,
                Antitoxin, Owner.Creature, null);
        }
    }
}

using RelationshipCore.Dynamics;

namespace RelationshipCore.Tests.Dynamics;

/// <summary>
/// Раунд правок 2, задача 2: заявленная "утечка" praise между владельцами (Admiration=0.20 у NPC3
/// при отсутствии записи praise₃(action 1)) не воспроизвелась ни через прямой вызов
/// SocialDynamicsEngine.Perceive, ни через живой UI-прогон того же сценария (UI Automation,
/// 2026-07-07) — Appraisal уже хранит praise per-instance (собственный Dictionary&lt;int,float&gt;
/// на каждый NpcState), лишний lookup не найден. Тесты ниже фиксируют это как regression-гарантию
/// ровно по формулировке приёмки задачи 2.
/// </summary>
public class AppraisalTests
{
    [Fact]
    public void GetPraise_NoRecordForAction_ReturnsZero()
    {
        var appraisal = new Appraisal();

        Assert.Equal(0f, appraisal.GetPraise(new ActionId(1)));
    }

    [Fact]
    public void GetPraise_RecordForDifferentAction_DoesNotAffectQueriedAction()
    {
        var appraisal = new Appraisal();
        appraisal.SetPraise(new ActionId(0), 0.00f);

        Assert.Equal(0f, appraisal.GetPraise(new ActionId(1)));
    }

    [Fact]
    public void GetPraise_RecordOnAnotherOwnersAppraisal_DoesNotAffectThisOwner()
    {
        var owner1 = new Appraisal();
        var owner3 = new Appraisal();
        owner1.SetPraise(new ActionId(1), 1.00f);

        Assert.Equal(0f, owner3.GetPraise(new ActionId(1)));
    }

    [Fact]
    public void GetAttitude_RecordOnAnotherOwnersAppraisal_DoesNotAffectThisOwner()
    {
        var owner1 = new Appraisal();
        var owner2 = new Appraisal();
        owner1.SetAttitude(2, 0.20f);

        Assert.Equal(0f, owner2.GetAttitude(2));
    }
}

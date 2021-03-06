using System;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

public class HerdingState : PersonState
{
    private HerdingShed Work;
    private Point WorkBase;
    private Point WorkSpot;

    private WorkState State;
    private float HarvestRate = 1f;
    private float NextHarvestTick = 0f;

    public HerdingState(Person person) : base(person.gameObject)
    {
        _person = person;
    }

    public override void OnStateEnter()
    {
        State = WorkState.MoveToResource;
        WorkBase = _person.CurrentPosition;
        Work = (HerdingShed)SimulationCore.Instance.AllWorkplaces[_person.Work];
        WorkSpot = Work.GrazingTiles.RandomItem();
        _person.Movement.GeneratePath(WorkBase, WorkSpot, GameSettings.WalkableTiles);
    }

    public override Type Tick()
    {
        switch (State)
        {
            case WorkState.MoveToResource:
                MoveToResource();
                break;
            case WorkState.Herding:
                DoingWork();
                break;
            case WorkState.MoveBackToBase:
                return ReturnResourcesToBase();
        }

        return typeof(HerdingState);
    }

    private void MoveToResource()
    {
        _person.Movement.MoveToDestination(GameSettings.WalkableTiles);
        if (gameObject.transform.position.IsSamePositionAs(GeneralUtility.GetLocalCenterOfCell(WorkSpot)))
        {
            // Arrived at thing to harvest.
            State = WorkState.Herding;
            NextHarvestTick = Time.time + HarvestRate;
        }

    }

    private void DoingWork()
    {
        if (NextHarvestTick < Time.time)
        {
            _person.Inventory.AddItem(ItemType.Wood, 1);
            if (_person.Inventory.BagIsFull)
            {
                _person.Movement.GeneratePath(WorkSpot, WorkBase, GameSettings.WalkableTiles);
                State = WorkState.MoveBackToBase;
                return;
            }
        } 
        if (_person.Needs.HasCriticalNeed())
        {
            _person.Movement.GeneratePath(WorkSpot, WorkBase, GameSettings.WalkableTiles);
            State = WorkState.MoveBackToBase;
        }
    }

    private Type ReturnResourcesToBase()
    {
        _person.Movement.MoveToDestination(GameSettings.WalkableTiles);
        if (gameObject.transform.position.IsSamePositionAs((GeneralUtility.GetLocalCenterOfCell(WorkBase))))
        {
            var amount = _person.Inventory.RemoveItem(ItemType.Wood);
            if (_person.Needs.HasCriticalNeed())
            {
                return typeof(MovingState);
            }
            _person.Movement.GeneratePath(WorkBase, WorkSpot, GameSettings.WalkableTiles);
            State = WorkState.MoveToResource;
        }
        return typeof(HerdingState);
    }

    public override string ToString()
    {
        return State.GetDescription();
    }

    private enum WorkState
    {
        [Description("Moving to resource")]
        MoveToResource,
        [Description("Herding sheep")]
        Herding,
        [Description("Moving back to base")]
        MoveBackToBase,
    }
}


// on state entered => findwork => if found, set workbasepoint and worktile
// tick check if inventory is full or hascriticalneed
// if true, return to base and dump inventory
// 
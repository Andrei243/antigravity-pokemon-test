using System;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The Week's Work read (plan 41 · EG6; style guide, "Menu screens (G10)", "The Week's Work"): the week's missions one to
/// a row, each with what it asks, its meter and what it pays, and BEGIN on a timed one not under way. Confirm on a mission
/// done claims it (its pennants to Pennant Hall, its item to the bag); on a timed one it is begun. Rewards a past week
/// left are claimed as the board opens. Its steps take no input of their own, so tests and the harness drive it.
/// </summary>
public sealed class MissionBoardScreen
{
    private const float AppearTime = 0.35f;

    public bool IsActive { get; private set; }
    public int Cursor { get; private set; }
    public float Age { get; private set; }

    public MissionBoard Board { get; private set; } = new();
    public PlazaState Plaza { get; private set; } = new();
    private Inventory bag = new();

    /// <summary>The pennants this visit paid out, which the script that opened the board hears.</summary>
    public int Paid { get; private set; }

    /// <summary>The last thing a press did, said under the board until the next.</summary>
    public string Notice { get; private set; } = "";

    public void Open(MissionBoard board, PlazaState plaza, Inventory inventory)
    {
        Board = board;
        Plaza = plaza;
        bag = inventory;
        IsActive = true;
        Age = 0f;
        Paid = 0;
        Notice = "";
        Cursor = Math.Max(0, board.Missions.FindIndex(m => m.IsDone && !m.Claimed));
        // What a past week left done and unclaimed is claimed at the board as it is read
        int waiting = board.Waiting.Count;
        if (waiting > 0)
        {
            int pennants = 0;
            foreach (var w in board.Waiting)
            {
                pennants += w.Pennants;
                if (w.Item != null && ItemDatabase.Get(w.Item) is { } item) bag.AddItem(item, 1);
            }
            board.Waiting.Clear();
            plaza.Give(pennants);
            Paid += pennants;
            Notice = Text.Of("hubMissions.notice.past", ("n", pennants));
        }
        AudioManager.PlaySound("menu_open");
    }

    public void Close()
    {
        IsActive = false;
        AudioManager.PlaySound("menu_close");
    }

    public Mission? Selected => Cursor >= 0 && Cursor < Board.Missions.Count ? Board.Missions[Cursor] : null;

    public void Move(int dy)
    {
        if (dy == 0 || Board.Missions.Count == 0) return;
        Cursor = UiNav.Wrap(Cursor, Math.Sign(dy), Board.Missions.Count);
        AudioManager.PlaySound("cursor");
    }

    /// <summary>The A button on the mission under the cursor: claims one done, begins a timed one, says why not otherwise.</summary>
    public void Confirm()
    {
        if (Selected is not { } m) return;
        if (Board.Claim(Cursor) is { } reward)
        {
            Plaza.Give(reward.Pennants);
            Paid += reward.Pennants;
            if (reward.Item != null && ItemDatabase.Get(reward.Item) is { } item)
            {
                bag.AddItem(item, 1);
                Notice = Text.Of("hubMissions.notice.claimedItem", ("n", reward.Pennants), ("item", item.DisplayName));
            }
            else Notice = Text.Of("hubMissions.notice.claimed", ("n", reward.Pennants));
            AudioManager.PlaySound("select");
            return;
        }
        if (m.IsTimed && !m.IsDone && Board.Begin(Cursor))
        {
            Notice = Text.Of("hubMissions.notice.begun", ("minutes", m.Minutes), ("region", RegionDatabase.Get(m.Region)?.Name ?? m.Region));
            AudioManager.PlaySound("select");
            return;
        }
        Notice = m.Claimed ? Text.Of("hubMissions.notice.alreadyClaimed")
            : m.IsTimed && Board.Running != null && Board.Running != m ? Text.Of("hubMissions.notice.oneAtATime")
            : m.IsTimed && Board.Running == m ? Text.Of("hubMissions.notice.underWay")
            : Text.Of("hubMissions.notice.notDone");
        AudioManager.PlaySound("error");
    }

    public void Cancel() => Close();

    public void Update(float dt)
    {
        if (!IsActive) return;
        Age += dt;
        int dy = InputManager.Axis(GameAction.Up, GameAction.Down);
        if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else if (dy != 0) Move(dy);
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive) return;
        ModernUi.DrawMissionBoard(screenWidth, screenHeight, this, Math.Clamp(Age / AppearTime, 0f, 1f));
    }
}

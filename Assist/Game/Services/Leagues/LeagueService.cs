﻿using System;
using System.Text.Json;
using System.Threading.Tasks;
using Assist.Game.Views.Leagues;
using Assist.ViewModels;
using AssistUser.Lib.Leagues.Models;
using AssistUser.Lib.Parties;
using AssistUser.Lib.Parties.Models;
using AssistUser.Lib.Profiles.Models;
using Avalonia.Threading;
using Serilog;

namespace Assist.Game.Services.Leagues;

public class LeagueService
{

    public static LeagueService Instance;
    
    public AssistProfile ProfileData;
    public string CurrentLeagueId { get; set; }
    public AssistParty CurrentPartyInfo { get; set; }
    public AssistLeague CurrentLeagueInfo { get; set; }
    private bool currentlyBinded = false;

    public LeagueNavigationController NavigationController = new LeagueNavigationController();
    public LeagueService()
    {
        new MatchService();
        if (Instance is null) Instance = this; else return;
        BindToEvents();     
    }

    public async Task<AssistProfile> GetProfileData()
    {
        var resp = await AssistApplication.Current.AssistUser.Profile.GetProfile();
        if (resp.Code != 200)
        {
            Log.Error("CANNOT GET PROFILE DATA ON LEAGUESERVICE");
            Log.Error(resp.Message);
        }

        ProfileData = JsonSerializer.Deserialize<AssistProfile>(resp.Data.ToString());
        return ProfileData;
    }
    
    public async Task<AssistParty> GetCurrentPartyData()
    {
        var resp = await AssistApplication.Current.AssistUser.Party.GetParty();
        if (resp.Code != 200)
        {
            Log.Error("CANNOT GET PARTY DATA ON LEAGUESERVICE");
            Log.Error(resp.Message);
            return new AssistParty();
        }

        CurrentPartyInfo = JsonSerializer.Deserialize<AssistParty>(resp.Data.ToString());
        return CurrentPartyInfo;
        
    }
    
    public async Task<AssistParty> CreateNewParty()
    {
        var resp = await AssistApplication.Current.AssistUser.Party.CreateParty(new CreateParty()
        {
            LeagueId = CurrentLeagueId,
        });
        if (resp.Code != 200)
        {
            Log.Error("CANNOT CREATE PARTY ON LEAGUESERVICE");
            Log.Error(resp.Message);
            return new AssistParty();
        }

        CurrentPartyInfo = JsonSerializer.Deserialize<AssistParty>(resp.Data.ToString());
        return CurrentPartyInfo;
    }
    
    
    public void BindToEvents()
    {
        if (currentlyBinded)return;
        AssistApplication.Current.GameServerConnection.PARTY_PartyUpdateReceived += GameServerConnectionOnPARTY_PartyUpdateReceived;
        AssistApplication.Current.GameServerConnection.PARTY_PartyKickReceived += GameServerConnectionOnPARTY_PartyKickReceived;
        AssistApplication.Current.GameServerConnection.QUEUE_InQueueMessageReceived += GameServerConnectionOnQUEUE_InQueueMessageReceived;
        AssistApplication.Current.GameServerConnection.QUEUE_LeaveQueueMessageReceived += GameServerConnectionOnQUEUE_LeaveQueueMessageReceived;
        AssistApplication.Current.GameServerConnection.MATCH_JoinedMatchMessageReceived += GameServerConnectionOnMATCH_JoinedMatchMessageReceived;
        currentlyBinded = !currentlyBinded;
    }
    
    public void UnbindToEvents()
    {
        AssistApplication.Current.GameServerConnection.PARTY_PartyUpdateReceived -= GameServerConnectionOnPARTY_PartyUpdateReceived;
        AssistApplication.Current.GameServerConnection.PARTY_PartyKickReceived -= GameServerConnectionOnPARTY_PartyKickReceived;
        AssistApplication.Current.GameServerConnection.QUEUE_InQueueMessageReceived -= GameServerConnectionOnQUEUE_InQueueMessageReceived;
        AssistApplication.Current.GameServerConnection.QUEUE_LeaveQueueMessageReceived -= GameServerConnectionOnQUEUE_LeaveQueueMessageReceived;
    }
    
    private void GameServerConnectionOnPARTY_PartyKickReceived(string? obj)
    {
        Log.Information("Kick Recieved from Party");
        
    }

    private void GameServerConnectionOnPARTY_PartyUpdateReceived(string? obj)
    {
        try
        {
            CurrentPartyInfo = JsonSerializer.Deserialize<AssistParty>(obj);
        }
        catch (Exception e)
        {
            Log.Error("Failed to Serialize Party data");
            Log.Error("Failed on LeagueService");
            Log.Error(e.Message);
            Log.Error(e.StackTrace);
        }
    }

    public async Task<AssistLeague> GetCurrentLeagueInformation()
    {
        var resp = await AssistApplication.Current.AssistUser.League.GetLeagueInfo(Instance.CurrentLeagueId);
        if (resp.Code != 200)
        {
            Log.Error("CANNOT GET LEAGUE DATA ON LEAGUESERVICE");
            Log.Error(resp.Message);
        }

        CurrentLeagueInfo = JsonSerializer.Deserialize<AssistLeague>(resp.Data.ToString());
        return CurrentLeagueInfo;
    }
    
    private void GameServerConnectionOnQUEUE_InQueueMessageReceived(object? obj)
    {
        Log.Error("InqueueMessageReceived");
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            GameViewNavigationController.Change(new QueuePage());
        });
    }
    
    private void GameServerConnectionOnQUEUE_LeaveQueueMessageReceived(object? obj)
    {
        Log.Error("LeaveMessageReceived");
        
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            GameViewNavigationController.Change(new LeagueMainPage());
        });
    }
    
    
    /// <summary>
    /// Handles Switching the UI Control to the Match Page.
    /// </summary>
    /// <param name="obj"></param>
    private async void GameServerConnectionOnMATCH_JoinedMatchMessageReceived(object? obj)
    {
        Log.Error("Match has Been Joined Message has been recieved.");
        Log.Error("Switching UI to MatchPage.");
        AssistApplication.Current.PlaySound("https://content.assistapp.dev/audio/709fe49c-293b-4cd6-987d-848304f28eee/MemberJoined.mp3");
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            GameViewNavigationController.Change(new MatchPage());
        });
    }
}
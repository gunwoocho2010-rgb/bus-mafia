using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusMafia.Models;

namespace BusMafia.Hubs
{
    public class GameHub : Hub
    {
        // 1. 학생들이 스마트폰으로 접속했을 때 실행되는 탑승 로직
        public async Task JoinGame(string playerName, int seatNumber)
        {
            // 중복 좌석 검사
            if (GameRoom.ConnectedPlayers.Any(p => p.SeatNumber == seatNumber))
            {
                await Clients.Caller.SendAsync("ReceiveSystemMessage", "⚠️ 이미 등록된 좌석 번호입니다.");
                return;
            }

            var newPlayer = new Player
            {
                ConnectionId = Context.ConnectionId,
                Name = playerName,
                SeatNumber = seatNumber
            };

            GameRoom.ConnectedPlayers.Add(newPlayer);

            // 버스 안의 모든 사람에게 실시간 전송
            await Clients.All.SendAsync("PlayerJoined", playerName, seatNumber, GameRoom.ConnectedPlayers.Count);
        }

        // 2. 방장(노트북)이 게임 시작을 누르면 작동하는 역할 분배 및 통보 알고리즘
        public async Task StartGame()
        {
            int totalCount = GameRoom.ConnectedPlayers.Count;
            if (totalCount == 0) return;

            GameRoom.CurrentGameState = "Day";

            // 인원수에 맞춰 마피아와 경찰 수 동적 조절 (유연한 인원 참가 가능)
            int mafiaCount = 1;
            int copCount = 1;

            if (totalCount >= 30) { mafiaCount = 3; copCount = 2; }
            else if (totalCount >= 15) { mafiaCount = 2; copCount = 1; }

            // Fisher-Yates 기반 무작위 셔플로 역할 분배
            Random rand = new Random();
            var shuffled = GameRoom.ConnectedPlayers.OrderBy(x => rand.Next()).ToList();
            
            for (int i = 0; i < shuffled.Count; i++)
            {
                if (i < mafiaCount) shuffled[i].Role = "마피아";
                else if (i < mafiaCount + copCount) shuffled[i].Role = "경찰";
                else shuffled[i].Role = "시민";
                
                shuffled[i].IsAlive = true;
            }

            // 각 개인의 스마트폰 비밀 채널로 역할 개별 통보
            foreach (var player in GameRoom.ConnectedPlayers)
            {
                await Clients.Client(player.ConnectionId).SendAsync("ReceiveRole", player.Role);
            }

            await Clients.All.SendAsync("ReceiveSystemMessage", "🚨 게임이 시작되었습니다! 스마트폰으로 역할을 확인하고 첫 번째 낮 토론을 시작하세요.");
            await Clients.All.SendAsync("ChangePhase", "Day");
        }

        // 3. 35인 익명 채팅 필터링 중계 기능
        public async Task SendAnonymousMessage(string message)
        {
            var player = GameRoom.ConnectedPlayers.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
            if (player == null || !player.IsAlive) return;

            // '낮' 상태일 때만 익명 대화 허용
            if (GameRoom.CurrentGameState == "Day")
            {
                string anonymousSender = $"익명_{player.SeatNumber}번 좌석";
                await Clients.All.SendAsync("ReceiveMessage", anonymousSender, message);
            }
        }

        // 4. 밤 투표 시스템 전환 및 처리
        public async Task SwitchToNight()
        {
            GameRoom.CurrentGameState = "Night";
            foreach (var p in GameRoom.ConnectedPlayers) p.VoteCount = 0; // 투표수 리셋

            await Clients.All.SendAsync("ReceiveSystemMessage", "🌙 밤이 되었습니다. 의심되는 좌석 번호를 투표하고 제출하세요!");
            await Clients.All.SendAsync("ChangePhase", "Night");
        }

        public async Task CastVote(int targetSeatNumber)
        {
            var voter = GameRoom.ConnectedPlayers.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
            if (voter == null || !voter.IsAlive) return;

            var target = GameRoom.ConnectedPlayers.FirstOrDefault(p => p.SeatNumber == targetSeatNumber);
            if (target != null && target.IsAlive)
            {
                target.VoteCount++;
                await Clients.Caller.SendAsync("ReceiveSystemMessage", $"✓ {targetSeatNumber}번 좌석에 투표했습니다.");
            }
        }

        public async Task SwitchToDay()
        {
            GameRoom.CurrentGameState = "Day";
            
            // 최고 투표를 받은 사람 정산 및 탈락 처리
            var suspect = GameRoom.ConnectedPlayers.Where(p => p.IsAlive).OrderByDescending(p => p.VoteCount).FirstOrDefault();
            string resultMessage = "지난밤에는 투표 부재 등으로 아무도 탈락하지 않았습니다.";
            
            if (suspect != null && suspect.VoteCount > 0)
            {
                suspect.IsAlive = false;
                resultMessage = $"🚨 투표 결과, {suspect.SeatNumber}번 좌석의 [{suspect.Name}] 친구가 탈락했습니다! (정체는 {suspect.Role}였습니다.)";
            }

            await Clients.All.SendAsync("ReceiveSystemMessage", resultMessage);
            await Clients.All.SendAsync("ReceiveSystemMessage", "☀️ 낮이 되었습니다. 다시 자유롭게 토론하세요!");
            await Clients.All.SendAsync("ChangePhase", "Day");

            CheckGameOver();
        }

        private void CheckGameOver()
        {
            int mafiaCount = GameRoom.ConnectedPlayers.Count(p => p.IsAlive && p.Role == "마피아");
            int citizenCount = GameRoom.ConnectedPlayers.Count(p => p.IsAlive && p.Role != "마피아");

            if (mafiaCount == 0)
            {
                Clients.All.SendAsync("ReceiveSystemMessage", "🎉 게임 종료! 시민 진영이 마피아를 모두 소탕하여 승리했습니다!");
                GameRoom.CurrentGameState = "Lobby";
            }
            else if (citizenCount <= mafiaCount)
            {
                Clients.All.SendAsync("ReceiveSystemMessage", "💀 게임 종료! 마피아가 시민을 압도하여 버스를 장점했습니다!");
                GameRoom.CurrentGameState = "Lobby";
            }
        }

        // 터널 통과 등으로 신호가 잠깐 끊겼을 때의 예외 처리
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var player = GameRoom.ConnectedPlayers.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
            if (player != null)
            {
                player.Note = "Disconnected";
                await Clients.All.SendAsync("ReceiveSystemMessage", $"⚠️ {player.SeatNumber}번 좌석의 신호가 불안정합니다.");
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
namespace BusMafia.Models
{
    public class Player
    {
        public string ConnectionId { get; set; } // 웹소켓 고유 연결 ID
        public string Name { get; set; }         // 학생 이름
        public int SeatNumber { get; set; }      // 버스 좌석 번호 (1~35)
        public string Role { get; set; } = "시민"; // 마피아, 경찰, 시민
        public bool IsAlive { get; set; } = true;// 생존 여부
        public int VoteCount { get; set; } = 0;  // 해당 라운드에 받은 투표수
        public string Note { get; set; }         // 연결 끊김 등 상태 메모용
    }
}
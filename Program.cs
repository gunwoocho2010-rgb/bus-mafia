using BusMafia.Hubs;

var builder = WebApplication.CreateBuilder(args);

// 1. 실시간 통신 기능(SignalR) 등록
builder.Services.AddSignalR();

var app = builder.Build();

// 🚨 [매우 중요] 클라우드(리눅스) 환경에서 index.html을 강제로 첫 화면으로 고정하는 코드
app.UseDefaultFiles(); 
app.UseStaticFiles();  // wwwroot 폴더를 읽어오기 위한 필수 코드

app.UseRouting();

// 2. 스마트폰 클라이언트가 접속할 관문 주소 매핑
app.MapHub<GameHub>("/gameHub");

// 3. 서버 실행
app.Run();
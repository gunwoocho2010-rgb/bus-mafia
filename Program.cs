using BusMafia.Hubs;

var builder = WebApplication.CreateBuilder(args);

// 실시간 웹소켓(SignalR) 서비스 관문 추가
builder.Services.AddSignalR();

var app = builder.Build();

app.UseStaticFiles(); // wwwroot 폴더 내부의 웹 화면을 전송하기 위한 필수 미들웨어
app.UseRouting();

// 스마트폰 클라이언트가 붙을 무선 기지국 주소 매핑
app.MapHub<GameHub>("/gameHub");

app.Run();
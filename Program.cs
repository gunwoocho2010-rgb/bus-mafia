var builder = WebApplication.CreateBuilder(args);

// 1. 실시간 통신을 위한 웹소켓(SignalR) 서비스 등록
builder.Services.AddSignalR();

var app = builder.Build();

// 🚨 [무조건 화면을 띄우는 핵심 치트키]
// 어떤 주소로 들어오든 강제로 wwwroot/index.html을 찾아가도록 윈도우/리눅스 공용 경로 설정
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();

// 2. 친구들 핸드폰이 통신할 관문 연결 (Hubs 폴더 안의 GameHub 클래스)
// 만약 네임스페이스 에러가 나면 아래처럼 직접 풀네임으로 매핑해 버리면 해결됩니다.
app.MapHub<BusMafia.Hubs.GameHub>("/gameHub");

// 3. 만약 index.html을 못 찾을 때를 대비한 '안전장치 백업 코드'
app.MapGet("/", async context =>
{
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.WriteAsync("<h1>🚌 버스 마피아 서버는 켜졌는데 index.html 파일이 누락되었습니다! wwwroot 폴더를 확인하세요.</h1>");
});

app.Run();
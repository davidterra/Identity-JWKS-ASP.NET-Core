# Identity-JWKS-ASP.NET-Core
Trabalhando com chaves assimétricas usando as bibliotecas: 

- NetDevPack.Security.JwtSigningCredentials.AspNetCore 
- NetDevPack.Security.JwtSigningCredentials.Store.EntityFrameworkCore

Com o uso de um JWKS podemos centralizar em um único enpoint nossa chave pública e privada, sendo que para acessar a chave a pública basta realizar a requisição adicionando a rota "/jwks". Em posse do endereço que contem a chave pública basta realizar o a configuração conforme códio abaixo:

```c#

services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = true;
    x.SaveToken = true;
    x.SetJwksOptions(new JwkOptions(https://localhost:5003/jwks));
});

```

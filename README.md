# Video Manager Service

Microsserviço responsável pelo gerenciamento de vídeos, incluindo upload, persistência, controle de status e disponibilização de frames processados.

## Visão Geral

O Video Manager Service atua como componente central no pipeline de processamento de vídeos, coordenando o fluxo entre upload, armazenamento, processamento e distribuição de frames extraídos. Implementado seguindo os princípios de **Clean Architecture**, garante separação de responsabilidades e independência de frameworks.

### Responsabilidades

- Recepção e armazenamento de vídeos via object storage (MinIO)
- Persistência de metadados em banco relacional (PostgreSQL)
- Controle de ciclo de vida e status de processamento
- Interface de callback para atualização de status pelo processador
- Disponibilização de frames processados em formato compactado
- Notificação de erros via integração com serviço de notificação

## Arquitetura

### Stack Tecnológica

- **.NET 9.0** com **C# 12**
- **ASP.NET Core** para API RESTful
- **Entity Framework Core** para persistência
- **PostgreSQL** como banco de dados
- **MinIO** (S3-compatible) para object storage
- **Swagger/OpenAPI** para documentação

### Estrutura do Projeto

```
src/
├── Domain/              # Entidades, enums, interfaces e exceções
├── Application/         # DTOs, interfaces e casos de uso
├── Infrastructure/      # Implementações de repositórios e serviços externos
└── Presentation/        # Controllers e configurações da API
```

### Diagrama de Fluxo

```
Cliente → Gateway → Video Manager → MinIO (armazenamento)
                         ↓
                    PostgreSQL (metadados)
                         ↓
                    Video Processor (callback)
                         ↓
                    Notification Service (erros)
```

## Pré-requisitos

- **.NET 9.0 SDK** ou superior
- **Docker** e **Docker Compose** (para execução via container)
- **PostgreSQL 16** (para execução local)
- **MinIO** (para execução local)

## Como Executar

### Opção 1: Executar com Docker Compose (Recomendado)

```bash
# 1. Navegar até o diretório do projeto
cd Hackaton_Oficial/fiap-x-microsservice-video-manager

# 2. Construir e iniciar todos os serviços (API, PostgreSQL, MinIO)
docker-compose up -d

# 3. Verificar logs (opcional)
docker-compose logs -f videomanager-api

# 4. Parar os serviços
docker-compose down
```

**Serviços disponíveis:**
- API: `http://localhost:5002`
- Swagger: `http://localhost:5002/swagger`
- MinIO Console: `http://localhost:9001` (minioadmin / minioadmin123)

### Opção 2: Executar Localmente

```bash
# 1. Restaurar dependências
dotnet restore

# 2. Navegar até o diretório do código-fonte
cd src

# 3. Executar a aplicação
dotnet run

# A API estará disponível em http://localhost:5002
```

**Nota:** Para execução local, configure PostgreSQL e MinIO manualmente ou ajuste as connection strings em `appsettings.json`.

### Opção 3: Executar com Docker (API apenas)

```bash
# 1. Construir a imagem Docker
docker build -t fiapx-video-manager .

# 2. Executar o container
docker run -d -p 5002:8080 --name video-manager-api fiapx-video-manager
```

### Verificar Funcionamento

```bash
# Testar se a API está respondendo
curl http://localhost:5002/swagger

# Ou acessar no navegador
# http://localhost:5002/swagger
```

## Endpoints

### Documentação Completa
Acesse `http://localhost:5002/swagger` para documentação interativa completa.

### Resumo dos Endpoints

#### 📤 Upload de Vídeo
**Endpoint:** `POST /api/videos/upload`  
**Content-Type:** `multipart/form-data`

**Parâmetros:**
- `userId` (string): ID do usuário
- `video` (file): Arquivo de vídeo

**Response (200 OK):**
```json
{
  "videoId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fileName": "video.mp4",
  "status": "Pending"
}
```

#### 📋 Listar Vídeos do Usuário
**Endpoint:** `GET /api/videos/user/{userId}`

**Response (200 OK):**
```json
[
  {
    "videoId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "fileName": "video.mp4",
    "uploadedAt": "2026-03-15T10:30:00Z",
    "status": "Completed",
    "frameCount": 120
  }
]
```

#### 📊 Consultar Status
**Endpoint:** `GET /api/videos/{videoId}/status`

**Response (200 OK):**
```json
{
  "videoId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "Completed",
  "frameCount": 120,
  "processedAt": "2026-03-15T10:35:00Z"
}
```

#### 🔄 Atualizar Status (Callback)
**Endpoint:** `PUT /api/videos/{videoId}/status`

**Request Body:**
```json
{
  "status": "Completed",
  "zipFileName": "frames.zip",
  "frameCount": 120,
  "errorMessage": null
}
```

#### 📥 Download de Frames
**Endpoint:** `GET /api/videos/{videoId}/download`

**Response:** Arquivo ZIP com frames processados

## Armazenamento

### PostgreSQL - Metadados
Armazena informações sobre os vídeos:
- ID, nome do arquivo, usuário
- Status de processamento
- Timestamps de upload e processamento
- Quantidade de frames e localização no MinIO

### MinIO - Object Storage
Estrutura de buckets:
```
fiapx-videos/
├── uploads/           # Vídeos originais enviados
│   └── {timestamp}_{guid}.mp4
└── outputs/           # Frames processados
    └── {videoId}/
        └── frames_{videoId}.zip
```

## Configuração

Arquivo `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres;Port=5432;Database=videomanager_db;Username=postgres;Password=postgres123"
  },
  "MinIO": {
    "Endpoint": "minio:9000",
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin123",
    "BucketName": "fiapx-videos",
    "UseSSL": false
  },
  "Services": {
    "NotificationService": {
      "Url": "http://notification-api:8080"
    }
  }
}
```

## Testes

### Executar Testes Unitários

```bash
# Navegar até o diretório de testes
cd tests

# Executar todos os testes
dotnet test

# Executar com cobertura de código
dotnet test --collect:"XPlat Code Coverage"

# Gerar relatório de cobertura (se coverlet estiver configurado)
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## Licença

MIT License

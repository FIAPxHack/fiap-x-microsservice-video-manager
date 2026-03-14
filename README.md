# FIAP X - Microsserviço Gerenciador de Vídeos (Video Manager)

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-12.0-239120?logo=csharp&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Clean Architecture](https://img.shields.io/badge/Architecture-Clean-blue)](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
[![MinIO](https://img.shields.io/badge/Storage-MinIO-C72E49?logo=minio&logoColor=white)](https://min.io/)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

Microsserviço responsável pelo **gerenciamento de vídeos**, incluindo upload, controle de status e download de frames processados. Desenvolvido com **ASP.NET Core 9.0**, **C#**, **Clean Architecture** e **MinIO** para armazenamento.

---

## 📋 Sobre o Projeto

O **fiap-x-video-manager** é um microsserviço RESTful que gerencia o ciclo de vida de vídeos na plataforma FIAP X:

### Funcionalidades Principais 

- ✅ **Upload de Vídeo** - Recebe vídeos via multipart/form-data e armazena no MinIO
- ✅ **Listar Vídeos** - Retorna todos os vídeos de um usuário
- ✅ **Consultar Status** - Verifica status de processamento (Pending, Processing, Completed, Failed)
- ✅ **Atualizar Status** - Endpoint para callback do video-processor atualizar status
- ✅ **Download de Frames** - Fornece arquivo ZIP com frames extraídos (do MinIO)
- ✅ **Integração com Notification Service** - Notifica usuário sobre erros de processamento

### Arquitetura de Processamento

```
┌──────────────┐      ┌──────────────┐      ┌──────────────┐
│   Usuário    │─────▶│    Gateway   │─────▶│Video Manager │
└──────────────┘      └──────────────┘      └──────┬───────┘
                                                     │
                                                     ▼
                                              ┌──────────┐
                                              │  MinIO   │ (Bucket)
                                              └────┬─────┘
                                                   │ Trigger/Queue
                                                   ▼
                                         ┌─────────────────┐
                                         │Video Processor  │
                                         │  (FFmpeg)       │
                                         └────────┬────────┘
                                                  │
                                       ┌──────────┴─────────┐
                                       ▼                    ▼
                                  Frames no MinIO    Callback PUT Status
```

### Responsabilidades

1. **Upload**: Recebe vídeo → Salva no MinIO → Registra no banco (status: Pending)
2. **Status**: Fornece informações sobre o vídeo e seu processamento
3. **Callback**: Recebe atualização de status do video-processor
4. **Download**: Fornece acesso aos frames processados (do MinIO)
5. **Notificação**: Chama notification-service em caso de erro

---

## 🏗️ Arquitetura Clean Architecture

```
┌─────────────────────────────────────────┐
│     Presentation (Controllers, API)     │
├─────────────────────────────────────────┤
│    Application (Use Cases, DTOs)        │
├─────────────────────────────────────────┤
│    Domain (Entities, Interfaces)        │
├─────────────────────────────────────────┤
│    Infrastructure (MinIO, PostgreSQL)   │
└─────────────────────────────────────────┘
```

---

## 🚀 Tecnologias

- **.NET 9.0** - Runtime e Framework
- **ASP.NET Core** - Web API
- **Clean Architecture** - Separação de camadas
- **PostgreSQL** - Banco de dados relacional
- **MinIO** - Object Storage (S3-compatible)
- **AWS SDK S3** - Cliente para MinIO
- **Swagger/OpenAPI** - Documentação da API

---

## 📦 Pré-requisitos

1. **.NET 9.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
2. **Docker & Docker Compose** - [Download](https://www.docker.com/)

---

## 🔧 Instalação e Execução

### Usando Docker Compose (Recomendado)

```bash
# Iniciar todos os serviços
docker-compose up -d

# Ver logs
docker-compose logs -f videomanager-api
```

Serviços disponíveis:
- **API**: http://localhost:5002
- **Swagger**: http://localhost:5002/swagger
- **MinIO Console**: http://localhost:9001 (minioadmin / minioadmin123)
- **pgAdmin**: http://localhost:5050 (admin@fiapx.com / admin123)

### Executando Localmente

```bash
cd src
dotnet restore
dotnet run
```

**Importante**: Configure PostgreSQL e MinIO localmente ou ajuste `appsettings.json`.

---

## 📡 Endpoints Principais

### Upload de Vídeo
```http
POST /api/videos/upload
Content-Type: multipart/form-data

userId: string
video: file
```

### Listar Vídeos do Usuário
```http
GET /api/videos/user/{userId}
```

### Consultar Status
```http
GET /api/videos/{videoId}/status
```

### Atualizar Status (Callback do Processor)
```http
PUT /api/videos/{videoId}/status
Content-Type: application/json

{
  "status": "Completed",
  "zipFileName": "frames.zip",
  "frameCount": 120,
  "errorMessage": null
}
```

### Download de Frames
```http
GET /api/videos/{videoId}/download
```

---

## 🗄️ Armazenamento MinIO

O serviço utiliza **MinIO** como object storage para:
- Armazenar vídeos enviados pelos usuários
- Armazenar frames processados pelo video-processor
- Compatibilidade com AWS S3 (fácil migração para produção)

### Estrutura do Bucket

```
fiapx-videos/
├── uploads/
│   └── {timestamp}_{guid}.mp4
└── outputs/
    └── {videoId}/
        └── frames_{videoId}.zip
```

---

## 🔗 Integração com Outros Serviços

### Video Processor (Desenvolvido pela equipe)
- Consome fila de vídeos pendentes
- Baixa vídeo do MinIO
- Processa com FFmpeg (extrai frames)
- Faz upload dos frames para MinIO
- **Chama PUT /api/videos/{id}/status** para atualizar

### Notification Service
- Chamado em caso de erro no processamento
- Envia e-mail para o usuário

### Gateway
- Roteia todas as requisições
- Aplica autenticação JWT
- Políticas de autorização

---

## 🧪 Testes

```bash
cd tests
dotnet test
```

---

## 📝 Variáveis de Ambiente

```env
ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=videomanager_db;...
MinIO__Endpoint=minio:9000
MinIO__AccessKey=minioadmin
MinIO__SecretKey=minioadmin123
MinIO__BucketName=fiapx-videos
MinIO__UseSSL=false
Services__NotificationService__Url=http://notification-api:8080
```

---

## 📄 Licença

MIT License - Ver arquivo [LICENSE](LICENSE)

---

## 👥 Equipe

Projeto desenvolvido para a pós-graduação FIAP - Arquitetura de Microsserviços

### Upload de Vídeo
```http
POST /api/videos/upload
Content-Type: multipart/form-data
```

### Listar Vídeos do Usuário
```http
GET /api/videos/user/{userId}
```

### Consultar Status
```http
GET /api/videos/{videoId}/status
```

### Download de Frames
```http
GET /api/videos/{videoId}/download
```

---

## 🔗 Integração

O serviço se integra com:
- **Notification Service** (porta 5001) - Para envio de emails

Configurar em `appsettings.json`:
```json
{
  "Services": {
    "NotificationService": {
      "Url": "http://localhost:5001"
    }
  }
}
```

---

## 📝 Licença

MIT License - Projeto FIAP X Pós-Graduação

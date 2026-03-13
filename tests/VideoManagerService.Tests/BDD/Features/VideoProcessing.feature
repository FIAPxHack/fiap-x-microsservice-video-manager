# language: pt
Funcionalidade: Upload e Gerenciamento de Vídeos
  Como um usuário
  Eu quero fazer upload de vídeos para processamento externo
  Para que sejam processados por serviço especializado

  Cenário: Upload de vídeo com sucesso
    Dado que sou um usuário com ID "user123" e email "test@example.com"
    Quando eu faço upload de um vídeo "video.mp4" de 10 MB
    Então o upload deve ser realizado com sucesso
    E devo receber um ID de vídeo

  Cenário: Upload de vídeo com formato inválido
    Dado que sou um usuário com ID "user456" e email "user@example.com"
    Quando eu tento fazer upload de um arquivo "document.pdf"
    Então o upload deve falhar
    E deve mostrar erro de "Formato de arquivo inválido"

  Cenário: Upload de vídeo muito grande
    Dado que sou um usuário com ID "user789" e email "bigfile@example.com"
    Quando eu tento fazer upload de um vídeo de 600 MB
    Então o upload deve falhar
    E deve mostrar erro de "Arquivo excede o tamanho máximo"

  Cenário: Listar vídeos do usuário
    Dado que sou um usuário com ID "user-list" e email "list@example.com"
    E que fiz upload de 3 vídeos anteriormente
    Quando eu consulto meus vídeos
    Então devo ver 3 vídeos na lista
    E todos devem pertencer ao usuário "user-list"

  Cenário: Consultar status de vídeo em processamento
    Dado que um vídeo com ID "video123" está em processamento
    Quando eu consulto o status do vídeo
    Então o status deve ser "Processing"

  Esquema do Cenário: Upload de diferentes formatos válidos
    Dado que sou um usuário com ID "<userId>" e email "<email>"
    Quando eu faço upload de um vídeo "<arquivo>"
    Então o upload deve ser realizado com sucesso
    E o formato deve ser aceito

    Exemplos:
      | userId | email            | arquivo     |
      | user1  | user1@test.com   | video.mp4   |
      | user2  | user2@test.com   | video.avi   |
      | user3  | user3@test.com   | video.mov   |
      | user4  | user4@test.com   | video.mkv   |

  Cenário: Consultar vídeo inexistente
    Quando eu consulto o status de um vídeo inexistente "fake-id"
    Então não deve encontrar o vídeo

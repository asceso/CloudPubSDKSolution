# CloudPub SDK

---
## Поддерживаемые платформы
Проект нацелен как минимум на .NET 6.0

---
## Руководство использования
1) Создать обьект CloudPubTunnel
2) Вызвать метод ```InitCloudPub```
3) Установить токен для работы через метод ```SetToken(string token)```
4) Открыть тунель через метод ```OpenTunnel(TunnelType tunnelType, int port, int maxSecondsAttemptToOpen = 10)```, возможные варианты TunnelType ```enum TunnelType { HTTP, HTTPS }```
5) В ответе метода OpenTunnel получаем публичный адрес с которым можно работать
6) Если необходимо закончить работу вызываем ```CloseTunnel```
7) Метод ```IsTunnelAlive``` позволяет получить состояние процесса (false если процесс выключен)
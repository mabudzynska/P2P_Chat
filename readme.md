# Projekt P2P CHAT:
Readme do poprawy - test z opisem
---
## Maszyna Stanów Aplikacji:
1.	START
    a.	GUI: Logowanie do Aplikacji – Wpisanie nazwy użykownika.
    b.	BACKEND: Inicjalizacja Serwisu UDP i TCP.
2.	DISCOVERY
    a.	UDP - Aplikacja wysyła na broadcast "HELLO" - przedstawiając się w ten sposób innym użytkownikom z nazwy, portu na którym będzie rozmawiać (TCP) oraz unikalnego w sieci adresu IP.
3.	CONNECTED
    a.	UDP - Rozsyłanie co 5 sekund ramki HELLO. Ma na celu podtrzymanie informacji u innych użytkowników a stanie naszej aktywności. Na podstawie braku tej ramki inni użytkownicy wykryją nasz timeout. Dodatkowo peer manager na podstawie ramki "HELLO" - Zapisuje nas do swoich "Kontaktów"
    b.	UDP – Stałe nasłuchwianie na tym samym porcie co nadawczy. Na podstawie ramek z HELLO dodajemy użytkowników lub usuwamy gdy nastąpi timeout.
  
    /TODO:
    c.	TCP – Wysyłanie wiadomości. Struktra ramki: Moje imie, mój IP, mój port TCP, Payload. JSON

4.	DISCONNECTED
    a. UDP - kończymy pracę z aplikacją. Na pożegnanie na broadcast idzie ramka z informacją GOODBYE - żeby nie timeoutować tylko od razu dać informację.


## Settings:
1. Timeout = 15s
2. UDP Port = 50000
3. TCP Port = undefined

* Ramki wysyłane są w formacie JSON, przykład:
{
    "Name":"Marta",
    "Type":0,
    "Port":54321,
    "payload":"WIADOMOŚĆ XYZ SSSSSSS"
}

gdzie 
- Name - nazwa użytkownika który wysyła ramkę
- Type - rodzaj ramki : powitanie, pożegnanie, wiadomość,
- Port - port operacyjny TCP źródła wiadomości
- payload - string z wiadomością.


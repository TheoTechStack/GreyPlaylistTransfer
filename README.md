# Playlist Transfer UI - Docker Guide

This guide provides instructions on how to build and run the `PlaylistTransfer.UI` application using Docker.

## Building the Docker Image

To build the Docker image, run the following command from the root directory of the project:

```sh
docker build -t playlisttransfer-ui -f PlaylistTransfer.UI/Dockerfile .
```
```sh
docker run -d -p 8080:8080 --name playlisttransfer-ui-container playlisttransfer-ui
```

<img width="1440" alt="Screenshot 2025-03-02 at 21 58 34" src="https://github.com/user-attachments/assets/506fc8f6-420b-42e8-a10e-96691cc3fe41" />
<img width="1440" alt="Screenshot 2025-03-02 at 21 58 34 1" src="https://github.com/user-attachments/assets/6f441c03-892e-40a1-96bd-315ed0a119a3" />
<img width="1440" alt="Screenshot 2025-03-02 at 21 59 07" src="https://github.com/user-attachments/assets/d8b4a943-37b6-4ff6-932f-3e94817b7bdb" />
<img width="1440" alt="Screenshot 2025-03-02 at 21 59 40" src="https://github.com/user-attachments/assets/b4c10a53-3ddc-48d8-9585-adfb3e74be93" />
<img width="1440" alt="Screenshot 2025-03-02 at 21 59 56" src="https://github.com/user-attachments/assets/90a7eb27-e531-480f-9703-2011688d938a" />

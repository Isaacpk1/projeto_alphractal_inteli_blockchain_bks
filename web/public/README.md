# public/ — arquivos servidos como estão

Favicon, imagens estáticas, `robots.txt`. O Vite copia o conteúdo para a raiz do
build sem processar.

Nada que precise de import ou hash de build vai aqui — isso vive em `src/` e passa
pelo bundler.

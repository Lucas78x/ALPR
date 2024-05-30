<?php
// Inicie a sessão
session_start();

// Verifique se o usuário está logado
if (!isset($_SESSION['username'])) {
    // Se o usuário não estiver logado, retorne uma resposta JSON indicando que o usuário está desconectado
    echo json_encode(['conectado' => false]);
    exit;
}

// Se o usuário estiver logado, atualize o tempo de atividade da sessão
$_SESSION['last_activity'] = time();

// Responda com uma resposta JSON indicando que o usuário está conectado
echo json_encode(['conectado' => true]);
?>
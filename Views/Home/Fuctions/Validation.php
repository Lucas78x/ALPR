<?php

function isLoggedIn($username) {
    if ($username === "") {
        return false;
    } else {
        return true;
    }
}

// Função para atualizar o tempo de atividade da sessão
function updateLastActivity() {
    $_SESSION['LAST_ACTIVITY'] = time();
}

// Função para recuperar o nome de usuário da sessão
function getUsername() {
    return $_SESSION['username'];
}
?>

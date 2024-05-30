<?php
// Define o nome do cookie a ser removido
$cookie_name = "username";

// Define o tempo de expiração do cookie para uma data passada (ex: 1 hora atrás)
setcookie($cookie_name, "", time() - 3600, "/");

// Redireciona para outra página
header("Location: ../../Login/index.php");
exit(); // Certifique-se de sair após o redirecionamento
?>
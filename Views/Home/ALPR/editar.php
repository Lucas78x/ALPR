<?php

// Verifica se os dados foram enviados via POST
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    // Recebe os dados do formulário
    $index = $_POST['id'];
    $nome = $_POST['nome'];
    $rtsp = $_POST['rtsp'];

    // Lê o conteúdo do arquivo JSON
    $json_data = file_get_contents('cameras.json');

    // Decodifica o JSON em um array associativo
    $data = json_decode($json_data, true);

    // Acessa os dados das câmeras
    $cameras = &$data['cameras']; // Referência para poder modificar o array original

    // Verifica se o índice da câmera existe no array
    if (array_key_exists($index, $cameras)) {
        // Atualiza os dados da câmera
        $cameras[$index]['nome'] = $nome;
        $cameras[$index]['rtsp'] = $rtsp;

// Codifica os dados de volta para JSON sem escapar barras ("/") nos URLs
$new_json_data = json_encode($data, JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES);

// Salva os dados de volta no arquivo JSON
file_put_contents('cameras.json', $new_json_data);

        // Retorna uma resposta de sucesso
        echo json_encode(array('success' => true));
    } else {
        // Retorna uma resposta de erro se o índice da câmera não existir
        echo json_encode(array('success' => false, 'message' => 'Índice da câmera inválido.'));
    }
} else {
    // Retorna uma resposta de erro se os dados não foram enviados via POST
    echo json_encode(array('success' => false, 'message' => 'Requisição inválida.'));
}
?>
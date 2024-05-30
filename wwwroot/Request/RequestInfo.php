<?php
function getAPIResponse($url) {
    // Faz a solicitação HTTP para a URL especificada
    $response = file_get_contents($url);

    // Verifica se houve algum erro na solicitação
    if ($response === false) {
        // Se houver um erro, retorna uma mensagem de erro
        return array('error' => "Erro ao fazer a solicitação HTTP.");
    } else {
        // Se a solicitação for bem-sucedida, decodifica a resposta JSON
        $data = json_decode($response, true);

        // Extrai as informações necessárias do JSON
        $totalCamLPR = isset($data['totalCamLPR']) ? $data['totalCamLPR'] : 0;
        $totalPlacasRegistradas = isset($data['totalPlacasRegistradas']) ? $data['totalPlacasRegistradas'] : 0;

        // Monta os dados de resposta em formato JSON
        return array(
            'totalCamLPR' => $totalCamLPR,
            'totalPlacasRegistradas' => $totalPlacasRegistradas
        );
    }
}
?>

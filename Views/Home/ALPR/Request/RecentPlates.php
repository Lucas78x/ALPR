<?php
$array_cameras = array("Guarajuba", "Monte Gordo", "Itacimirim", "Praia do Forte", "Residence", "Balli Bahia");
$array_datas = array("09:01:59", "08:01:59", " 10:01:59", "11:01:59","12:01:59", "13:01:59");
$array_placas = array("ABC1234", "XYZ5678","XYZ5679", "XYZ5658","XYZ5458", "XYZ5358");
$array_modelos = array("Sedan", "SUV", "Chevrolet", "Honda","Corsa","Celta");
return;

// URL da API
$url = 'sua_url_da_api_aqui';

// Fazendo a requisição para a API
$response = file_get_contents($url);

// Verificando se a requisição foi bem-sucedida
if ($response === false) {
 return;
}

// Convertendo a resposta da API para um array associativo
$data = json_decode($response, true);

// Arrays para armazenar os dados
$array_datas = array();
$array_placas = array();
$array_modelos = array();

// Preenchendo os arrays com os dados da API
foreach ($data as $item) {
    // Convertendo a data para o formato desejado (d/m/Y)
    $date = date("d/m/Y", strtotime($item['date']));
    
    // Adicionando a data, a placa e o modelo aos arrays correspondentes
    $array_datas[] = $date;
    $array_placas[] = $item['plate'];
    $array_modelos[] = $item['model'];
}
?>

drop table baking_reward;
drop table finalization_data_finalizers;
drop table transaction_summary;
drop table finalization_reward;


ALTER TABLE block
    drop column parent_block,           
    drop column block_last_finalized,   
    drop column genesis_index,          
    drop column era_block_height,       
    drop column block_receive_time,     
    drop column block_arrive_time,      
    drop column block_slot,             
    drop column block_slot_time,        
    drop column block_baker,            
    drop column finalized,              
    drop column transaction_count,      
    drop column transaction_energy_cost,
    drop column transaction_size,       
    drop column block_state_hash;